# AI-Powered Content Moderation Service

An intelligent Docker container with advanced NLP capabilities for context-aware content moderation and sentiment analysis.

## 🧠 AI Features

- **Intent Analysis**: Understands actual meaning, not just keywords (e.g., "I don't hate you" vs "I hate you")
- **Context Understanding**: Uses spaCy NLP to analyze sentence structure and meaning
- **Negation Detection**: Recognizes when harmful words are negated ("never", "don't", "wouldn't")
- **Smart Sentiment Analysis**: VADER + TextBlob for social media optimized sentiment detection
- **Pattern Matching**: Traditional keyword-based detection as backup
- **Risk Assessment**: Combines multiple analysis methods for accurate scoring
- **System Tag Suggestions**: Maps content to predefined system tag categories
- **REST API**: Simple HTTP endpoints for integration
- **Health Checks**: Built-in monitoring endpoints

## 🎯 Intelligence Examples

**Context-Aware Analysis:**
- ✅ "I don't hate you, I love you" → SAFE (understands negation + positive intent)
- ❌ "I will hurt you" → DANGEROUS (recognizes actual threat)
- ✅ "I would never commit violence" → SAFE (negated harmful intent)
- ❌ "Violence is the answer" → FLAGGED (asserted harmful intent)

## Quick Start

### Using Docker Compose (Recommended)

```bash
docker-compose up -d
```

### Using Docker

```bash
# Build the image
docker build -t sentiment-analysis .

# Run the container
docker run -p 8000:8000 sentiment-analysis
```

## API Usage

### Content Moderation (Primary Endpoint)

```bash
curl -X POST http://localhost:8000/moderate \
  -H "Content-Type: application/json" \
  -d '{"text": "I dont hate you, I would never commit violence against you, I love you"}'
```

Response with **Intent Analysis**:
```json
{
  "text": "I dont hate you, I would never commit violence against you, I love you",
  "sentiment": {
    "label": "POSITIVE",
    "confidence": 0.95,
    "source": "vader_textblob"
  },
  "suggested_tags": {
    "ContentWarning": ["Violence"],
    "Violation": ["Harassment", "Hate Speech"]
  },
  "intent_analysis": {
    "intent_analysis_available": true,
    "overall_risk": "LOW",
    "confidence": 0,
    "detected_intents": [],
    "intent_details": {
      "violence": {
        "detected": false,
        "confidence": 0,
        "context": "Negated: 'I dont hate you, I would never commit violence against you, I love you'"
      },
      "harassment": {
        "detected": false,
        "confidence": 0,
        "context": "Negated: 'I dont hate you, I would never commit violence against you, I love you'"
      },
      "hate": {
        "detected": false,
        "confidence": 0,
        "context": "Negated: 'I dont hate you, I would never commit violence against you, I love you'"
      }
    },
    "summary": "No concerning intent detected"
  },
  "risk_assessment": {
    "score": 0.085,
    "level": "MINIMAL",
    "pattern_score": 0.085,
    "intent_score": 0
  },
  "requires_review": false
}
```

### Batch Content Moderation

```bash
curl -X POST http://localhost:8000/batch-moderate \
  -H "Content-Type: application/json" \
  -d '{"texts": ["Normal content", "Inappropriate content"], "include_sentiment": true}'
```

### Legacy Sentiment Analysis

```bash
curl -X POST http://localhost:8000/analyze \
  -H "Content-Type: application/json" \
  -d '{"text": "I love this product!"}'
```

Response:
```json
{
  "text": "I love this product!",
  "sentiment": "POSITIVE",
  "confidence": 0.9998
}
```

### Health Check

```bash
curl http://localhost:8000/health
```

## System Tag Categories

The service detects content patterns and suggests appropriate system tags:

### Content Warning (Visible to Users)
- **NSFW**: Adult/explicit content
- **Violence**: Violent content
- **Sensitive**: Triggering content
- **Spoiler**: Spoiler content

### Violation (Hidden from Users)
- **Harassment**: Bullying, intimidation
- **Hate Speech**: Discriminatory content
- **Misinformation**: False information

### Quality (Hidden from Users)
- **Spam**: Commercial spam, repeated content
- **Low Quality**: Very short or meaningless posts

### Safety (Hidden from Users)
- **Self Harm**: Suicide/self-harm content
- **Doxxing**: Personal information exposure

## 🤖 AI Models & Technology

### Sentiment Analysis
- **VADER**: Rule-based sentiment analysis optimized for social media text
- **TextBlob**: Naive Bayes classifier for additional sentiment accuracy
- **Combined Scoring**: Weighted combination for better results

### Intent Analysis
- **spaCy**: Advanced NLP with `en_core_web_sm` model (~50MB)
- **Context Understanding**: Sentence structure and dependency parsing
- **Negation Detection**: Identifies negated harmful statements
- **Intent Classification**: Violence, harassment, hate speech, threats

### Risk Assessment
- **Pattern Matching**: Traditional keyword-based detection
- **Intent Override**: AI analysis takes precedence over pattern matching
- **Confidence Scoring**: 0.0 to 1.0 with detailed explanations
- **Risk Levels**: MINIMAL, LOW, MEDIUM, HIGH

## 🚀 Performance

- **Memory Usage**: ~300-500MB (much lighter than transformer models)
- **CPU**: Optimized for CPU inference, no GPU required
- **Latency**: ~100-300ms per request (including NLP processing)
- **Batch Limit**: 100 texts per batch request
- **Stability**: No segmentation faults or memory crashes

## 🔧 Integration Example

```python
import requests

def moderate_content_with_ai(text):
    """
    Moderate content using AI-powered intent analysis
    """
    response = requests.post(
        "http://localhost:8000/moderate",
        json={"text": text, "include_sentiment": True}
    )
    result = response.json()

    # Check if AI intent analysis is available
    if result.get("intent_analysis", {}).get("intent_analysis_available"):
        # Use AI analysis
        intent_risk = result["intent_analysis"]["confidence"]
        detected_intents = result["intent_analysis"]["detected_intents"]

        if intent_risk > 0.5:
            print(f"⚠️  High risk content detected: {detected_intents}")
            print(f"Context: {result['intent_analysis']['summary']}")
        else:
            print("✅ Content appears safe based on intent analysis")
    else:
        # Fallback to pattern matching
        pattern_risk = result["risk_assessment"]["pattern_score"]
        if pattern_risk > 0.5:
            print(f"⚠️  Flagged by pattern matching: {pattern_risk}")

    return result

# Example usage
safe_text = "I don't hate you, I love you"
harmful_text = "I will hurt you"

print("Testing safe text:")
moderate_content_with_ai(safe_text)

print("\nTesting harmful text:")
moderate_content_with_ai(harmful_text)
```

## 🧪 Testing Examples

```bash
# Test negated harmful content (should be SAFE)
curl -X POST http://localhost:8000/moderate \
  -H "Content-Type: application/json" \
  -d '{"text": "I would never hurt anyone, I hate violence"}'

# Test actual threat (should be DANGEROUS)
curl -X POST http://localhost:8000/moderate \
  -H "Content-Type: application/json" \
  -d '{"text": "I will hurt you and commit violence"}'

# Test sentiment analysis
curl -X POST http://localhost:8000/analyze \
  -H "Content-Type: application/json" \
  -d '{"text": "I absolutely love this feature!"}'
```

## 🆚 Pattern Matching vs AI Intent Analysis

| Scenario | Pattern Matching | AI Intent Analysis | Winner |
|----------|------------------|-------------------|---------|
| "I don't hate you" | ❌ FLAGGED (contains "hate") | ✅ SAFE (negated) | 🧠 AI |
| "I love violence in movies" | ❌ FLAGGED (contains "violence") | ✅ SAFE (context: movies) | 🧠 AI |
| "I will never hurt anyone" | ❌ FLAGGED (contains "hurt") | ✅ SAFE (negated intent) | 🧠 AI |
| "I will hurt you" | ✅ FLAGGED (contains "hurt") | ❌ DANGEROUS (actual threat) | 🤝 Both |
| "Violence is wrong" | ❌ FLAGGED (contains "violence") | ✅ SAFE (anti-violence statement) | 🧠 AI |

## 🔍 Health Check

```bash
curl http://localhost:8000/health
```

Response shows all available capabilities:
```json
{
  "status": "healthy",
  "model": "VADER + TextBlob",
  "sentiment_analysis": "lightweight-ai-enabled",
  "ai_enabled": true,
  "models": {
    "vader": true,
    "textblob": true,
    "spacy": true,
    "intent_analysis": true
  },
  "capabilities": {
    "sentiment_analysis": true,
    "pattern_matching": true,
    "intent_analysis": true,
    "context_understanding": true,
    "negation_detection": true
  }
}
```

## 🚀 Deployment

The service automatically deploys to staging when changes are pushed to any branch with `sentiment-analysis/**` modifications.

### Local Development
```bash
# Build and run locally
docker build -t yapplr-sentiment .
docker run -p 8000:8000 yapplr-sentiment

# Or use docker-compose
docker-compose up -d
```

### Production Deployment
The service integrates with the main Yapplr application and deploys automatically via GitHub Actions.

# Usage
result = moderate_content("This post contains inappropriate content")
print(f"Risk Level: {result['risk_assessment']['level']}")
print(f"Requires Review: {result['requires_review']}")
print(f"Suggested Tags: {result['suggested_tags']}")
```
