using LeakChecker.DataParser.Stats.Execution;

namespace LeakChecker.DataParser.Orchestration.Parsing;

public interface IParsingOrchestratorFactory
{
    ParsingOrchestrator Create(string filePath, ExecutionStats executionStats);
}