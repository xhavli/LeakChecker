namespace LeakChecker.DataParser.Orchestration.Parsing;

public interface IParsingOrchestratorFactory
{
    ParsingOrchestrator Create(string filePath, Guid executionId);
}