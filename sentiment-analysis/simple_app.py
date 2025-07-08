from flask import Flask, request, jsonify
import logging
import os

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__)

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({"status": "healthy", "model": "simple-test"}), 200

@app.route('/test', methods=['POST'])
def test_endpoint():
    """Simple test endpoint"""
    try:
        data = request.get_json()
        logger.info(f"Received data: {data}")
        
        if not data or 'text' not in data:
            return jsonify({"error": "Missing 'text' field in request"}), 400
        
        text = data['text']
        if not text.strip():
            return jsonify({"error": "Text cannot be empty"}), 400
        
        return jsonify({
            "text": text,
            "length": len(text),
            "message": "Test successful"
        })
        
    except Exception as e:
        logger.error(f"Error in test endpoint: {str(e)}")
        return jsonify({"error": "Internal server error"}), 500

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 8001))
    app.run(host='0.0.0.0', port=port, debug=True)
