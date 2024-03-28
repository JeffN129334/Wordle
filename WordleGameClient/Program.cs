using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Spectre.Console;
using WordleGameServer;

namespace WordleGameClient
{
    public class Program
    {
        
        static async Task Main(string[] args)
        {
            //Connect to the WordleGameServer
            var channel = GrpcChannel.ForAddress("https://localhost:7103");
            var wordle = new DailyWordle.DailyWordleClient(channel);

            List<(string Guess, string Result)> guessesResults = new List<(string Guess, string Result)>();
            List<char> availableLetters = "abcdefghijklmnopqrstuvwxyz".ToList();

            for (int i = 0; i < 6; i++)
            {
                guessesResults.Add(("_____", "")); // Pre-populate with placeholder guesses and empty results
            }

            using (var call = wordle.Play())
            {
                //Create an empty request and a blank string
                PlayRequest playRequest = new PlayRequest() { Word = "" };

                int turnsUsed = 0;
                bool gameWon = false;

                AnsiConsole.WriteLine("\nType in a guess, or hit enter to quit!");
                while (!gameWon && turnsUsed < 6)
                {
                    AnsiConsole.Clear();
                    DisplayMainMenu(availableLetters);
                    DisplayGuessesResultsTable(guessesResults);

                    string guess = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter your guess (5-letter word):[/]")
                            .Validate(input =>
                                input.Length == 5 ? ValidationResult.Success() : ValidationResult.Error("[red]Your guess must be exactly 5 letters[/]")));

                    await call.RequestStream.WriteAsync(new PlayRequest { Word = guess });
                    turnsUsed++;

                    if (await call.ResponseStream.MoveNext())
                    {
                        var serverMessage = call.ResponseStream.Current.Message;
                        gameWon = ParseAndDisplayServerMessage(serverMessage, guess, availableLetters, out string formattedResult);
                        guessesResults[turnsUsed - 1] = (guess, formattedResult);
                    }

                    if (gameWon)
                    {
                        AnsiConsole.Clear();
                        DisplayGuessesResultsTable(guessesResults);
                        AnsiConsole.MarkupLine("[bold green]Congratulations! You've guessed the word correctly![/]");
                        break; // Exit the loop if the game is won
                    }
                }

                if (!gameWon)
                {
                    AnsiConsole.Clear();
                    DisplayGuessesResultsTable(guessesResults);
                    AnsiConsole.MarkupLine("[bold red]Game over! You've used all your guesses.[/]");
                }

                // Ensure the request stream is complete
                await call.RequestStream.CompleteAsync();

                //Display user stats (which is currently a placeholder string)
                Console.WriteLine("User Stats: ");
                GetStatsResponse statRes = wordle.GetStats(new Empty());
                Console.WriteLine(statRes.Message);


                await channel.ShutdownAsync();
                Console.WriteLine("\nAll done. Press a key to exit.");
                Console.ReadKey();
            }
        }

        static void DisplayGuessesResultsTable(List<(string guess, string result)> guessesResults)
        {

            var resultsTable = new Table().Border(TableBorder.Rounded);
            resultsTable.AddColumn(new TableColumn(""));
            resultsTable.AddColumn("Guess");
            resultsTable.AddColumn("Result");

            int guessNumber = 1;
            foreach (var (guess, result) in guessesResults)
            {

                resultsTable.AddRow(guessNumber.ToString(), guess, result);
                guessNumber++;
            }

            // Style the table
            resultsTable.Border(TableBorder.Rounded).BorderColor(Color.Blue);
            resultsTable.LeftAligned();
            resultsTable.Collapse();

            AnsiConsole.Write(resultsTable);
        }
    


        static string FormatGuessResult(string hintResponse)
        {
            string formattedResult = "";
            foreach (var letter in hintResponse)
            {
                switch (letter)
                {
                    case '*':
                        formattedResult += "[green]*[/]";
                        break;
                    case '?':
                        formattedResult += "[yellow]?[/]";
                        break;
                    case 'x':
                        formattedResult += "[red]x[/]";
                        break;
                    default:
                        formattedResult += letter;
                        break;
                }
            }
            return formattedResult;
        }

        static bool ParseAndDisplayServerMessage(string message, string guess, List<char> availableLetters, out string formattedResult)
        {
            var lines = message.Split('\n');
            bool gameWon = false;
            string hint = "";

            foreach (var line in lines)
            {
                if (line.StartsWith("Hint:"))
                {
                    hint = line.Substring(5).Trim();
                }
                else if (line.StartsWith("Correct: True"))
                {
                    gameWon = true;
                }
            }

            // Update available letters based on 'x' hints
            for (int i = 0; i < guess.Length; i++)
            {
                if (i < hint.Length && hint[i] == 'x')
                {
                    availableLetters.Remove(guess[i]); // Removes the first occurrence of the letter
                }
            }

            formattedResult = FormatGuessResult(hint);
            return gameWon;
        }

        static void DisplayMainMenu(List<char> availableLetters)
        {
            // Create a table
            var table = new Table();

            // Add a column
            table.AddColumn(new TableColumn(new Markup("[bold]W O R D L E[/]")).LeftAligned());

            // Add rows with the game instructions
            table.AddRow("");
            table.AddRow("You have 6 chances to guess a 5-letter word.");
            table.AddEmptyRow();
            table.AddRow("Each guess must be a 'playable' 5 letter word.");
            table.AddEmptyRow();
            table.AddRow("After a guess the game will display a series of");
            table.AddRow("characters to show you how good your guess was.");
            table.AddEmptyRow();
            table.AddRow("[red]x[/] - means the letter above is not in the word.");
            table.AddRow("[yellow]?[/] - means the letter should be in another spot.");
            table.AddRow("[green]*[/] - means the letter is correct in this spot.");
            table.AddEmptyRow();
            //table.AddRow("Letters Available\na,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z");
            table.AddRow($"Available Letters:\n[bold]{string.Join(" ", availableLetters.OrderBy(c => c))}[/]");
            table.AddEmptyRow();

            // Style the table
            table.Border(TableBorder.Rounded).BorderColor(Color.Blue);
            table.LeftAligned();
            table.Collapse();

            // Render the table to the console
            AnsiConsole.Write(table);
        }
    }
}