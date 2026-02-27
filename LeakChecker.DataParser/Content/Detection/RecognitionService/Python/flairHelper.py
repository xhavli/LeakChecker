from presidio_analyzer import AnalyzerEngine, RecognizerRegistry, RecognizerResult
from typing import List
from flairConfig import FlairRecognizer

# Build once at module load (when config is imported)
flair_recognizer = FlairRecognizer(supported_language="en")
registry = RecognizerRegistry()
registry.add_recognizer(flair_recognizer)
analyzer = AnalyzerEngine(registry=registry)

def AnalyzeWithFlair(text: str, language: str = "en") -> List[RecognizerResult]:
    """
    Analyze text with the prebuilt AnalyzerEngine (Flair inside).
    """
    results = analyzer.analyze(
        text=text,
        language=language,
        return_decision_process=True,
    )

    return results
