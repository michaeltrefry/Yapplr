from flask import Flask, request, jsonify
import logging
import os
import re
from typing import Dict, List, Tuple
from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer
from textblob import TextBlob
import spacy

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__)

# Initialize lightweight sentiment analysis tools
logger.info("Content moderation service starting...")
logger.info("Loading sentiment analysis models...")

try:
    # Initialize VADER sentiment analyzer (rule-based, very fast and stable)
    vader_analyzer = SentimentIntensityAnalyzer()

    # Test VADER
    test_vader = vader_analyzer.polarity_scores("This is a test.")
    logger.info(f"VADER test successful: {test_vader}")

    # Initialize TextBlob (uses Naive Bayes, also lightweight)
    test_blob = TextBlob("This is a test.")
    test_textblob = test_blob.sentiment
    logger.info(f"TextBlob test successful: polarity={test_textblob.polarity}, subjectivity={test_textblob.subjectivity}")

    # Initialize spaCy for advanced NLP (context understanding, negation detection)
    try:
        nlp = spacy.load("en_core_web_sm")
        test_doc = nlp("I don't hate you")
        logger.info(f"spaCy test successful: {len(test_doc)} tokens processed")
        spacy_available = True
    except Exception as spacy_error:
        logger.warning(f"spaCy model not available: {spacy_error}")
        nlp = None
        spacy_available = False

    logger.info("Successfully loaded sentiment analysis models")
    sentiment_models_available = True

except Exception as e:
    logger.error(f"Failed to load sentiment models: {str(e)}")
    logger.info("Falling back to pattern-based sentiment analysis only")
    vader_analyzer = None
    nlp = None
    sentiment_models_available = False
    spacy_available = False

logger.info("Content moderation service ready!")

# System tag categories and their keywords/patterns
SYSTEM_TAG_PATTERNS = {
    "ContentWarning": {
        "NSFW": [
            r"\b(nsfw|not safe for work|adult content|explicit|sexual|nude|naked|porn|xxx)\b",
            r"\b(18\+|mature content|adult only)\b"
        ],
        "Violence": [
            r"\b(violence|violent|kill|murder|death|blood|gore|fight|attack|assault|weapon|gun|knife|bomb)\b",
            r"\b(war|battle|shooting|stabbing|beating|torture)\b"
        ],
        "Sensitive": [
            r"\b(trigger|triggering|sensitive|depression|anxiety|suicide|self harm|mental health)\b",
            r"\b(trauma|ptsd|abuse|eating disorder|addiction)\b"
        ],
        "Spoiler": [
            r"\b(spoiler|spoilers|plot twist|ending|finale|dies|death scene)\b",
            r"\b(season \d+|episode \d+|chapter \d+).*\b(reveals?|twist|surprise)\b"
        ]
    },
    "Violation": {
        "Harassment": [
            r"\b(harass|harassment|bully|bullying|intimidate|threaten|stalk|stalking)\b",
            r"\b(you suck|kill yourself|kys|loser|idiot|stupid|moron)\b"
        ],
        "Hate Speech": [
            r"\b(hate|racist|racism|sexist|sexism|homophobic|transphobic|bigot|nazi)\b",
            r"\b(slur|offensive|discriminat|prejudice)\b"
        ],
        "Misinformation": [
            r"\b(fake news|conspiracy|hoax|lie|lies|false|misinformation|disinformation)\b",
            r"\b(covid.*fake|vaccine.*dangerous|election.*stolen)\b"
        ]
    },
    "Quality": {
        "Spam": [
            r"\b(buy now|click here|free money|get rich|make money fast|limited time)\b",
            r"\b(viagra|casino|lottery|winner|congratulations.*won)\b",
            r"(http[s]?://[^\s]+){3,}",  # Multiple links
            r"(.)\1{10,}"  # Repeated characters
        ],
        "Low Quality": [
            r"^.{1,10}$",  # Very short posts
            r"^[^a-zA-Z]*$",  # No letters (only symbols/numbers)
            r"\b(first|second|third|fourth|fifth)\b$"  # Just ordinal numbers
        ]
    },
    "Safety": {
        "Self Harm": [
            r"\b(suicide|kill myself|end it all|self harm|cut myself|overdose|jump off)\b",
            r"\b(want to die|better off dead|no point living|end my life)\b"
        ],
        "Doxxing": [
            r"\b\d{3}-\d{3}-\d{4}\b",  # Phone numbers
            r"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",  # Email addresses
            r"\b\d{1,5}\s+\w+\s+(street|st|avenue|ave|road|rd|drive|dr|lane|ln|boulevard|blvd)\b",  # Addresses
            r"\b(home address|phone number|social security|ssn|credit card)\b"
        ]
    }
}

def analyze_sentiment_with_ai(text: str) -> Dict[str, any]:
    """
    Analyze sentiment using lightweight AI models with fallback to pattern-based analysis
    Returns sentiment result with label and confidence
    """
    if sentiment_models_available and vader_analyzer is not None:
        try:
            # Validate input text
            if not text or len(text.strip()) == 0:
                return {'label': 'NEUTRAL', 'score': 0.5, 'confidence': 0.5, 'source': 'empty_text'}

            # Clean and prepare text
            clean_text = text.strip()

            # Use VADER for primary analysis (great for social media text)
            vader_scores = vader_analyzer.polarity_scores(clean_text)

            # Use TextBlob as secondary analysis
            blob = TextBlob(clean_text)
            textblob_polarity = blob.sentiment.polarity

            # Combine both analyses for better accuracy
            # VADER gives us compound score (-1 to 1)
            # TextBlob gives us polarity (-1 to 1)

            vader_compound = vader_scores['compound']

            # Weight VADER more heavily as it's better for social media
            combined_score = (vader_compound * 0.7) + (textblob_polarity * 0.3)

            # Determine sentiment label
            if combined_score >= 0.1:
                label = 'POSITIVE'
                confidence = min(abs(combined_score) + 0.5, 0.95)
            elif combined_score <= -0.1:
                label = 'NEGATIVE'
                confidence = min(abs(combined_score) + 0.5, 0.95)
            else:
                label = 'NEUTRAL'
                confidence = 0.5 + (0.3 * (1 - abs(combined_score)))

            return {
                'label': label,
                'score': confidence,
                'confidence': confidence,
                'source': 'vader_textblob',
                'details': {
                    'vader_compound': vader_compound,
                    'textblob_polarity': textblob_polarity,
                    'combined_score': combined_score
                }
            }

        except Exception as e:
            logger.warning(f"Lightweight AI sentiment analysis failed: {str(e)}, falling back to pattern-based")

    # Fallback to simple pattern-based sentiment analysis
    return analyze_sentiment_patterns(text)

def analyze_sentiment_patterns(text: str) -> Dict[str, any]:
    """
    Simple pattern-based sentiment analysis as fallback
    """
    text_lower = text.lower()

    # Simple positive/negative word lists
    positive_words = [
        'good', 'great', 'excellent', 'amazing', 'wonderful', 'fantastic', 'awesome',
        'love', 'like', 'enjoy', 'happy', 'pleased', 'satisfied', 'perfect', 'best',
        'brilliant', 'outstanding', 'superb', 'magnificent', 'incredible', 'beautiful'
    ]

    negative_words = [
        'bad', 'terrible', 'awful', 'horrible', 'disgusting', 'hate', 'dislike',
        'angry', 'frustrated', 'disappointed', 'worst', 'pathetic', 'useless',
        'stupid', 'ridiculous', 'annoying', 'irritating', 'boring', 'ugly'
    ]

    positive_count = sum(1 for word in positive_words if word in text_lower)
    negative_count = sum(1 for word in negative_words if word in text_lower)

    if positive_count > negative_count:
        confidence = min(0.6 + (positive_count - negative_count) * 0.1, 0.9)
        return {'label': 'POSITIVE', 'score': confidence, 'confidence': confidence, 'source': 'pattern_based'}
    elif negative_count > positive_count:
        confidence = min(0.6 + (negative_count - positive_count) * 0.1, 0.9)
        return {'label': 'NEGATIVE', 'score': confidence, 'confidence': confidence, 'source': 'pattern_based'}
    else:
        return {'label': 'NEUTRAL', 'score': 0.5, 'confidence': 0.5, 'source': 'pattern_based'}

def analyze_intent_with_context(text: str) -> Dict[str, any]:
    """
    Advanced intent analysis that understands context, negation, and actual meaning
    """
    if not spacy_available or nlp is None:
        return {"intent_analysis_available": False, "reason": "spaCy not available"}

    try:
        doc = nlp(text)

        # Analyze for problematic intent patterns with context awareness
        intent_flags = {
            "violence": {"detected": False, "confidence": 0.0, "context": ""},
            "harassment": {"detected": False, "confidence": 0.0, "context": ""},
            "hate": {"detected": False, "confidence": 0.0, "context": ""},
            "threat": {"detected": False, "confidence": 0.0, "context": ""}
        }

        # Define problematic terms with their categories
        problematic_terms = {
            "violence": ["kill", "murder", "attack", "violence", "hurt", "harm", "beat", "fight"],
            "harassment": ["harass", "bully", "stalk", "intimidate", "threaten"],
            "hate": ["hate", "despise", "loathe", "detest"],
            "threat": ["will kill", "going to hurt", "watch out", "you're dead"]
        }

        # Check each sentence for context
        for sent in doc.sents:
            sent_text = sent.text.lower()

            # Look for negation patterns
            negation_words = ["not", "never", "don't", "won't", "wouldn't", "can't", "cannot", "no"]
            has_negation = any(neg in sent_text for neg in negation_words)

            # Check for problematic terms in context
            for category, terms in problematic_terms.items():
                for term in terms:
                    if term in sent_text:
                        # If negated, this is actually GOOD (expressing non-violence)
                        if has_negation:
                            intent_flags[category]["confidence"] = max(0, intent_flags[category]["confidence"] - 0.3)
                            intent_flags[category]["context"] = f"Negated: '{sent.text.strip()}'"
                        else:
                            # Positive assertion of problematic intent
                            intent_flags[category]["detected"] = True
                            intent_flags[category]["confidence"] = min(1.0, intent_flags[category]["confidence"] + 0.7)
                            intent_flags[category]["context"] = f"Asserted: '{sent.text.strip()}'"

        # Determine overall intent
        max_confidence = max(flag["confidence"] for flag in intent_flags.values())
        detected_intents = [category for category, flag in intent_flags.items() if flag["detected"]]

        return {
            "intent_analysis_available": True,
            "overall_risk": "HIGH" if max_confidence > 0.6 else "MEDIUM" if max_confidence > 0.3 else "LOW",
            "confidence": max_confidence,
            "detected_intents": detected_intents,
            "intent_details": intent_flags,
            "summary": f"Detected {len(detected_intents)} concerning intent(s)" if detected_intents else "No concerning intent detected"
        }

    except Exception as e:
        logger.warning(f"Intent analysis failed: {str(e)}")
        return {"intent_analysis_available": False, "reason": str(e)}

def analyze_content_patterns(text: str) -> Dict[str, List[str]]:
    """
    Analyze text for patterns that match system tag categories
    Returns a dictionary of category -> list of matched tags
    """
    text_lower = text.lower()
    matches = {}

    for category, tags in SYSTEM_TAG_PATTERNS.items():
        category_matches = []
        for tag_name, patterns in tags.items():
            for pattern in patterns:
                if re.search(pattern, text_lower, re.IGNORECASE):
                    category_matches.append(tag_name)
                    break  # Only add each tag once per category

        if category_matches:
            matches[category] = category_matches

    return matches

def get_content_risk_score(sentiment_result: Dict, pattern_matches: Dict[str, List[str]]) -> Tuple[float, str]:
    """
    Calculate a risk score based on sentiment and pattern matches
    Returns (risk_score, risk_level)
    """
    base_score = 0.0

    # Sentiment contribution (enhanced with AI confidence)
    if sentiment_result['label'] == 'NEGATIVE':
        # Use the actual confidence from AI model or pattern analysis
        confidence_multiplier = sentiment_result.get('confidence', sentiment_result.get('score', 0.5))
        base_score += confidence_multiplier * 0.4  # Max 0.4 from negative sentiment
    elif sentiment_result['label'] == 'POSITIVE':
        # Positive sentiment slightly reduces risk
        confidence_multiplier = sentiment_result.get('confidence', sentiment_result.get('score', 0.5))
        base_score -= confidence_multiplier * 0.1  # Slight risk reduction

    # Pattern match contributions
    risk_weights = {
        'ContentWarning': 0.2,  # Lower risk, just needs warning
        'Violation': 0.8,       # High risk
        'Quality': 0.4,         # Medium risk
        'Safety': 1.0           # Highest risk
    }

    for category, tags in pattern_matches.items():
        if category in risk_weights:
            base_score += len(tags) * risk_weights[category] * 0.1

    # Cap at 1.0
    risk_score = min(base_score, 1.0)

    # Determine risk level
    if risk_score >= 0.8:
        risk_level = "HIGH"
    elif risk_score >= 0.5:
        risk_level = "MEDIUM"
    elif risk_score >= 0.2:
        risk_level = "LOW"
    else:
        risk_level = "MINIMAL"

    return risk_score, risk_level

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    if sentiment_models_available:
        model_status = "lightweight-ai-enabled"
        model_name = "VADER + TextBlob"
    else:
        model_status = "pattern-based-fallback"
        model_name = "pattern-based"

    return jsonify({
        "status": "healthy",
        "model": model_name,
        "sentiment_analysis": model_status,
        "ai_enabled": sentiment_models_available,
        "models": {
            "vader": vader_analyzer is not None,
            "textblob": True,  # TextBlob is always available if imported
            "spacy": spacy_available,
            "intent_analysis": spacy_available
        },
        "capabilities": {
            "sentiment_analysis": True,
            "pattern_matching": True,
            "intent_analysis": spacy_available,
            "context_understanding": spacy_available,
            "negation_detection": spacy_available
        }
    }), 200

@app.route('/analyze', methods=['POST'])
def analyze_sentiment():
    """
    Analyze sentiment of provided text (legacy endpoint for backward compatibility)

    Expected JSON payload:
    {
        "text": "Your text to analyze"
    }

    Returns:
    {
        "text": "Your text to analyze",
        "sentiment": "POSITIVE" or "NEGATIVE",
        "confidence": 0.95
    }
    """
    try:
        data = request.get_json()

        if not data or 'text' not in data:
            return jsonify({"error": "Missing 'text' field in request"}), 400

        text = data['text']
        if not text.strip():
            return jsonify({"error": "Text cannot be empty"}), 400

        # Analyze sentiment using AI
        sentiment_result = analyze_sentiment_with_ai(text)

        return jsonify({
            "text": text,
            "sentiment": sentiment_result['label'],
            "confidence": round(sentiment_result['confidence'], 4),
            "source": sentiment_result.get('source', 'unknown')
        })

    except Exception as e:
        logger.error(f"Error analyzing sentiment: {str(e)}")
        return jsonify({"error": "Internal server error"}), 500

@app.route('/moderate', methods=['POST'])
def moderate_content():
    """
    Analyze content for moderation and suggest system tags
    """
    try:
        data = request.get_json()

        if not data or 'text' not in data:
            return jsonify({"error": "Missing 'text' field in request"}), 400

        text = data['text']
        if not text.strip():
            return jsonify({"error": "Text cannot be empty"}), 400

        include_sentiment = data.get('include_sentiment', True)

        # Simple response for now to test basic functionality
        response = {
            "text": text,
            "suggested_tags": {},
            "risk_assessment": {
                "score": 0.1,
                "level": "LOW"
            },
            "requires_review": False
        }

        # Analyze content patterns (basic keyword matching)
        pattern_matches = analyze_content_patterns(text)

        # Analyze intent with context (advanced NLP)
        intent_analysis = analyze_intent_with_context(text)

        # Analyze sentiment with AI
        sentiment_result = analyze_sentiment_with_ai(text) if include_sentiment else {'label': 'NEUTRAL', 'score': 0.5, 'confidence': 0.5}

        # Calculate risk score based on patterns and sentiment
        risk_score, risk_level = get_content_risk_score(
            sentiment_result,
            pattern_matches
        )

        # Combine pattern matching with intent analysis for better accuracy
        final_risk_score = risk_score
        requires_review = risk_score >= 0.5

        # If intent analysis is available, use it to override pattern matching
        if intent_analysis.get("intent_analysis_available", False):
            intent_risk = intent_analysis.get("confidence", 0)
            detected_intents = intent_analysis.get("detected_intents", [])

            # Intent analysis takes precedence over pattern matching
            if intent_risk > 0.3:  # Intent analysis detected concerning content
                final_risk_score = max(final_risk_score, intent_risk)
                requires_review = True
            elif intent_risk == 0 and detected_intents == []:  # Intent analysis says it's safe
                # Reduce risk score from pattern matching if intent analysis says it's safe
                final_risk_score = min(final_risk_score, 0.3)

        # Update response with comprehensive analysis
        response["suggested_tags"] = pattern_matches
        response["intent_analysis"] = intent_analysis
        response["risk_assessment"] = {
            "score": round(final_risk_score, 4),
            "level": "HIGH" if final_risk_score >= 0.7 else "MEDIUM" if final_risk_score >= 0.4 else "LOW" if final_risk_score >= 0.2 else "MINIMAL",
            "pattern_score": round(risk_score, 4),
            "intent_score": round(intent_analysis.get("confidence", 0), 4) if intent_analysis.get("intent_analysis_available") else None
        }
        response["requires_review"] = requires_review

        # Add real sentiment analysis
        if include_sentiment:
            response["sentiment"] = {
                "label": sentiment_result['label'],
                "confidence": round(sentiment_result['confidence'], 4),
                "source": sentiment_result.get('source', 'unknown')
            }

        return jsonify(response)

    except Exception as e:
        logger.error(f"Error moderating content: {str(e)}")
        return jsonify({"error": "Internal server error"}), 500

@app.route('/batch-moderate', methods=['POST'])
def batch_moderate_content():
    """
    Analyze multiple pieces of content for moderation

    Expected JSON payload:
    {
        "texts": ["text1", "text2", ...],
        "include_sentiment": true  // optional, defaults to true
    }

    Returns:
    {
        "results": [
            {
                "text": "text1",
                "sentiment": {...},
                "suggested_tags": {...},
                "risk_assessment": {...},
                "requires_review": true
            },
            ...
        ]
    }
    """
    try:
        data = request.get_json()

        if not data or 'texts' not in data:
            return jsonify({"error": "Missing 'texts' field in request"}), 400

        texts = data['texts']
        if not isinstance(texts, list):
            return jsonify({"error": "'texts' must be an array"}), 400

        include_sentiment = data.get('include_sentiment', True)

        results = []
        for text in texts:
            if not text or not text.strip():
                # Handle empty text
                result = {
                    "text": text,
                    "suggested_tags": {},
                    "risk_assessment": {
                        "score": 0.0,
                        "level": "MINIMAL"
                    },
                    "requires_review": False
                }
                if include_sentiment:
                    empty_sentiment = analyze_sentiment_with_ai("") if include_sentiment else {'label': 'NEUTRAL', 'score': 0.5, 'confidence': 0.5}
                    result["sentiment"] = {
                        "label": empty_sentiment['label'],
                        "confidence": round(empty_sentiment['confidence'], 4),
                        "source": empty_sentiment.get('source', 'unknown')
                    }
                results.append(result)
                continue

            # Analyze content patterns
            pattern_matches = analyze_content_patterns(text)

            # Analyze sentiment with AI
            sentiment_result = analyze_sentiment_with_ai(text) if include_sentiment else {'label': 'NEUTRAL', 'score': 0.5, 'confidence': 0.5}

            # Calculate risk score based on patterns and sentiment
            risk_score, risk_level = get_content_risk_score(
                sentiment_result,
                pattern_matches
            )

            # Create response for this text
            result = {
                "text": text,
                "suggested_tags": pattern_matches,
                "risk_assessment": {
                    "score": round(risk_score, 4),
                    "level": risk_level
                },
                "requires_review": (
                    risk_score >= 0.5 or  # Medium+ risk
                    any(category in ['Violation', 'Safety'] for category in pattern_matches.keys())
                )
            }

            # Add real sentiment analysis
            if include_sentiment:
                result["sentiment"] = {
                    "label": sentiment_result['label'],
                    "confidence": round(sentiment_result['confidence'], 4),
                    "source": sentiment_result.get('source', 'unknown')
                }

            results.append(result)

        return jsonify({"results": results})

    except Exception as e:
        logger.error(f"Error batch moderating content: {str(e)}")
        return jsonify({"error": "Internal server error"}), 500

@app.route('/batch-analyze', methods=['POST'])
def batch_analyze_sentiment():
    """
    Analyze sentiment of multiple texts (legacy endpoint)

    Expected JSON payload:
    {
        "texts": ["Text 1", "Text 2", "Text 3"]
    }
    """
    try:
        data = request.get_json()

        if not data or 'texts' not in data:
            return jsonify({"error": "Missing 'texts' field in request"}), 400

        texts = data['texts']
        if not isinstance(texts, list) or len(texts) == 0:
            return jsonify({"error": "texts must be a non-empty list"}), 400

        # Analyze all texts with AI
        response = []
        for text in texts:
            sentiment_result = analyze_sentiment_with_ai(text)
            response.append({
                "text": text,
                "sentiment": sentiment_result['label'],
                "confidence": round(sentiment_result['confidence'], 4),
                "source": sentiment_result.get('source', 'unknown')
            })

        return jsonify({"results": response})

    except Exception as e:
        logger.error(f"Error in batch analysis: {str(e)}")
        return jsonify({"error": "Internal server error"}), 500



if __name__ == '__main__':
    # This is only used for local development
    # In production, use: gunicorn --bind 0.0.0.0:8000 app:app
    port = int(os.environ.get('PORT', 8000))
    app.run(host='0.0.0.0', port=port, debug=False)
