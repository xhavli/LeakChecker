using LeakChecker.DataParser.Helpers.FileHelp;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Interfaces;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Execution;

namespace LeakChecker.DataParser.Orchestration.Parsing;

public class ParsingOrchestratorFactory(
    ISettings settings,
    FileHelper fileHelper,
    ExecutionLogger logger,
    IParseLoggerFactory parseLoggerFactory,
    IDatabase database)
    : IParsingOrchestratorFactory
{
    public ParsingOrchestrator Create(string filePath, ExecutionStats executionStats)
    {
        return new ParsingOrchestrator(
            filePath,
            settings,
            fileHelper,
            executionStats,
            logger,
            parseLoggerFactory,
            database);
    }
}