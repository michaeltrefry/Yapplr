# Content Moderation Service

A lightweight Docker container running DistilBERT for CPU-only content moderation and sentiment analysis.

## Features

- **Small Model**: DistilBERT (~250MB) - fine-tuned for sentiment analysis
- **CPU Only**: No GPU required, optimized for CPU inference
- **Content Moderation**: Automatic detection of problematic content patterns
- **System Tag Suggestions**: Maps content to predefined system tag categories
- **Risk Assessment**: Calculates risk scores and determines review requirements
- **REST API**: Simple HTTP endpoints for integration
- **Health Checks**: Built-in monitoring endpoints

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
  -d '{"text": "This is some inappropriate content with violence and harassment"}'
```

Response:
```json
{
  "text": "This is some inappropriate content with violence and harassment",
  "sentiment": {
    "label": "NEGATIVE",
    "confidence": 0.8542
  },
  "suggested_tags": {
    "ContentWarning": ["Violence"],
    "Violation": ["Harassment"]
  },
  "risk_assessment": {
    "score": 0.7234,
    "level": "HIGH"
  },
  "requires_review": true
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

## Model Details

- **Model**: `distilbert-base-uncased-finetuned-sst-2-english`
- **Size**: ~250MB
- **Sentiment Labels**: POSITIVE, NEGATIVE
- **Risk Levels**: MINIMAL, LOW, MEDIUM, HIGH
- **Confidence**: 0.0 to 1.0

## Performance

- **Memory Usage**: ~512MB-1GB
- **CPU**: Optimized for CPU inference
- **Latency**: ~50-200ms per request (depending on text length)
- **Batch Limit**: 100 texts per batch request

## Integration Example

```python
import requests

def moderate_content(text):
    response = requests.post(
        "http://localhost:8000/moderate",
        json={"text": text}
    )
    return response.json()

# Usage
result = moderate_content("This post contains inappropriate content")
print(f"Risk Level: {result['risk_assessment']['level']}")
print(f"Requires Review: {result['requires_review']}")
print(f"Suggested Tags: {result['suggested_tags']}")
```
