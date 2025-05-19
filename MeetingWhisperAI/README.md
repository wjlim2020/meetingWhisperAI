# MeetingWhisperAI

A C# Web API application that combines Whisper.NET for speech-to-text transcription with Microsoft's Semantic Kernel and Ollama for local AI-powered meeting analysis.

## Prerequisites

1. .NET 8.0 SDK or later
2. Ollama installed and running locally
3. Whisper model file (ggml-large-v1.bin)

## Setup

1. Clone this repository
2. Install Ollama from [ollama.ai](https://ollama.ai)
3. Pull the Qwen model in Ollama:
   ```bash
   ollama pull qwen2.5:7b
   ```
4. Download the Whisper model file (ggml-large-v1.bin) from [Hugging Face](https://huggingface.co/ggerganov/whisper.cpp) and place it in the project directory

## Running the Application

1. Make sure Ollama is running locally (default port: 11434)
2. Build and run the application:
   ```bash
   dotnet build
   dotnet run
   ```
3. Open your browser and navigate to `https://localhost:5001/swagger` to access the Swagger UI

## API Endpoints

### POST /api/audio/process
Upload and process an MP3 audio file.

**Request:**
- Content-Type: multipart/form-data
- Body: MP3 file upload

**Response:**
```json
{
  "transcript": "The full transcription of the audio",
  "analysis": "AI analysis of the meeting content"
}
```

**Example using curl:**
```bash
curl -X POST "https://localhost:5001/api/audio/process" \
     -H "accept: application/json" \
     -H "Content-Type: multipart/form-data" \
     -F "file=@your-meeting.mp3"
```

## Features

- RESTful API with Swagger documentation
- Local speech-to-text transcription using Whisper.NET
- MP3 to WAV conversion using NAudio
- Local AI-powered meeting analysis using Ollama
- Support for English language audio
- Automatic transcription saving to TXT file
- Completely local processing - no API keys required

## Models Used

- Whisper: ggml-large-v1.bin (high-accuracy speech recognition)
- Ollama: qwen2.5:7b (powerful language model for analysis)

## Notes

- The application uses the large Whisper model for better transcription accuracy
- For best results, use high-quality audio recordings
- The AI analysis is performed using Qwen 2.5 7B model through Ollama
- Make sure Ollama is running before starting the application
- Temporary WAV files are automatically cleaned up after processing
- The API supports large file uploads (configured for maximum size) 