from flask import Flask, request, jsonify
import logging
import os
import re
from typing import Dict, List, Tuple

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__)

# Initialize the sentiment analysis pipeline (disabled for now due to compatibility issues)
# This uses DistilBERT fine-tuned for sentiment analysis (~250MB)
logger.info("Content moderation service starting...")
sentiment_pipeline = None  # Disabled for now
logger.info("Pattern-based content moderation ready!")

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

    # Sentiment contribution
    if sentiment_result['label'] == 'NEGATIVE':
        base_score += sentiment_result['score'] * 0.3  # Max 0.3 from sentiment

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
    return jsonify({"status": "healthy", "model": "distilbert-content-moderation"}), 200

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

        # Analyze sentiment
        result = sentiment_pipeline(text)[0]

        return jsonify({
            "text": text,
            "sentiment": result['label'],
            "confidence": round(result['score'], 4)
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

        # Analyze content patterns
        pattern_matches = analyze_content_patterns(text)

        # Calculate risk score based on patterns
        risk_score, risk_level = get_content_risk_score(
            {'label': 'NEUTRAL', 'score': 0.5},
            pattern_matches
        )

        # Update response with actual analysis
        response["suggested_tags"] = pattern_matches
        response["risk_assessment"] = {
            "score": round(risk_score, 4),
            "level": risk_level
        }
        response["requires_review"] = (
            risk_score >= 0.5 or  # Medium+ risk
            any(category in ['Violation', 'Safety'] for category in pattern_matches.keys())
        )

        # Add sentiment placeholder (ML model disabled for now)
        if include_sentiment:
            response["sentiment"] = {
                "label": "NEUTRAL",
                "confidence": 0.5,
                "note": "ML sentiment analysis temporarily disabled"
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
                    result["sentiment"] = {
                        "label": "NEUTRAL",
                        "confidence": 0.5,
                        "note": "ML sentiment analysis temporarily disabled"
                    }
                results.append(result)
                continue

            # Analyze content patterns
            pattern_matches = analyze_content_patterns(text)

            # Calculate risk score based on patterns
            risk_score, risk_level = get_content_risk_score(
                {'label': 'NEUTRAL', 'score': 0.5},
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

            # Add sentiment placeholder (ML model disabled for now)
            if include_sentiment:
                result["sentiment"] = {
                    "label": "NEUTRAL",
                    "confidence": 0.5,
                    "note": "ML sentiment analysis temporarily disabled"
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

        # Analyze all texts
        results = sentiment_pipeline(texts)

        response = []
        for i, result in enumerate(results):
            response.append({
                "text": texts[i],
                "sentiment": result['label'],
                "confidence": round(result['score'], 4)
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
