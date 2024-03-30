﻿using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Spectre.Console;
using WordleGameServer;

namespace WordleGameClient
{
    //Authors: Jeff Nesbitt, Andrew Mattice, Gui Miranda
    //Purpose: A version of the game WordleTM involving a pair of gRPC services and console client.
    //Date: 2024/03/28
    //Project: Wordle
    public class Program
    {

        static async Task Main(string[] args)
        {
            //Connect to the WordleGameServer
            var channel = GrpcChannel.ForAddress("https://localhost:7103");
            var wordle = new DailyWordle.DailyWordleClient(channel);

            List<(string Guess, string Result)> guessesResults = new List<(string Guess, string Result)>();

            List<char> availableLetters = "abcdefghijklmnopqrstuvwxyz".ToList();
            Dictionary<char, char> letterStates = new Dictionary<char, char>();

            for (int i = 0; i < 6; i++)
            {
                guessesResults.Add(("_____", "")); // Pre-populate with placeholder guesses and empty results
            }

            Table mainMenuTable = new Table();
            Table guessResultsTable = new Table();

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

                    mainMenuTable = DisplayMainMenu(availableLetters, letterStates);
                    guessResultsTable = DisplayGuessesResultsTable(guessesResults);
             
                    AnsiConsole.Write(mainMenuTable);
                    AnsiConsole.Write(guessResultsTable);


                    bool isValidWord = false;
                    while (!isValidWord)
                    {
                        string guess = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter your guess (5-letter word):[/]")
                            .Validate(input =>
                                input.Length == 5 ? ValidationResult.Success() : ValidationResult.Error("[red]Your guess must be exactly 5 letters[/]")));

                        await call.RequestStream.WriteAsync(new PlayRequest { Word = guess });




                        if (await call.ResponseStream.MoveNext())
                        {
                            var serverMessage = call.ResponseStream.Current.Message;

                            if (serverMessage.Contains("Invalid word"))
                            {
                                AnsiConsole.Write(new Panel("[bold red]Invalid word. Try again.[/]").BorderColor(Color.Red));
                                continue; // Prompt again without incrementing turnsUsed
                            }
                            gameWon = ParseServerMessage(serverMessage, guess, availableLetters, letterStates, out string formattedResult);
                            guessesResults[turnsUsed] = (guess, formattedResult);
                            isValidWord = true; // Break the while loop and proceed
                        }
                    }

                        turnsUsed++;

                        if (gameWon)
                        {
                            AnsiConsole.Clear();

                            // Display a congratulatory message
                            AnsiConsole.Write(new FigletText("Congratulations!").Centered().Color(Color.Green));
                            Thread.Sleep(1000);
                            AnsiConsole.Clear();

                            AnsiConsole.Write(new Panel("[bold green]You've guessed the word correctly![/]").BorderColor(Color.Green).Expand());
                            guessResultsTable = DisplayGuessesResultsTable(guessesResults);


                        break; // Exit the loop if the game is won
                        }
                    }

                    if (!gameWon)
                    {
                        AnsiConsole.Clear();
                        AnsiConsole.Write(new FigletText("GAME OVER!").Centered().Color(Color.Red));
                        Thread.Sleep(1000);

                    AnsiConsole.Clear();
                    AnsiConsole.Write(new Panel("[bold red]You've used all your guesses.[/]").BorderColor(Color.Red).Expand());
                        guessResultsTable = DisplayGuessesResultsTable(guessesResults);
                    }

                    // Ensure the request stream is complete
                    await call.RequestStream.CompleteAsync();

                    //Display user stats
                    GetStatsResponse statRes = wordle.GetStats(new Empty());

                    Table statsTable = DisplayStats(statRes.Message);

                // Render the layout
                AnsiConsole.Write(guessResultsTable);
                AnsiConsole.Write(statsTable);

                await channel.ShutdownAsync();
                    Console.WriteLine("\nAll done. Press a key to exit.");
                    Console.ReadKey();
                }
            }

        //Purpose: Displays the Table containing the users guess and the results of their guess
        //Parameters: List<(string guess, string result)> guessesResults
        //Returns: Table
        static Table DisplayGuessesResultsTable(List<(string guess, string result)> guessesResults)
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

                return resultsTable;
            }

        //Purpose: Formats the results of the users guess for display purposes
        //Parameters: string hintResponse
        //Returns: string
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
                            formattedResult += "[gold3_1]?[/]";
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

        //Purpose: Parses the server message so it is easier to work with and checks if the game was won
        //Parameters: string message, string guess, List<char> availableLetters, Dictionary<char, char> letterStates, out string formattedResult
        //Returns: bool
        static bool ParseServerMessage(string message, string guess, List<char> availableLetters, Dictionary<char, char> letterStates, out string formattedResult)
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

                for (int i = 0; i < guess.Length; i++)
                {
                    char currentLetter = guess[i];
                    if (i < hint.Length)
                    {
                        char hintChar = hint[i];
                        if (hintChar == '*' || hintChar == '?')
                        {
                            letterStates[currentLetter] = hintChar;
                        }
                        else if (hintChar == 'x')
                        {
                         letterStates[currentLetter] = hintChar;
                    }
                    }
                }

                formattedResult = FormatGuessResult(hint);
                return gameWon;
            }

        //Purpose: Displays the main menu and the letters the user can use
        //Parameters: List<char> availableLetters, Dictionary<char, char> letterStates
        //Returns: Table
        static Table DisplayMainMenu(List<char> availableLetters, Dictionary<char, char> letterStates)
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
                table.AddRow("[gold3_1]?[/] - means the letter should be in another spot.");
                table.AddRow("[green]*[/] - means the letter is correct in this spot.");
                table.AddEmptyRow();
                table.AddRow("Available Letters:");
                // Define all letters for display
                string allLetters = "abcdefghijklmnopqrstuvwxyz";

                // Generate the display string for letters
                string lettersDisplay = string.Join(" ", allLetters.Select(letter =>
                    letterStates.ContainsKey(letter) ?
                    (
                        letterStates[letter] == '*' ? $"[green]{letter}[/]" :
                        letterStates[letter] == '?' ? $"[gold3_1]{letter}[/]" :
                        letterStates[letter] == 'x' ? $"[red]{letter}[/]" : $"[white]{letter}[/]"
                    ) :
                    $"[white]{letter}[/]" // Default color for unguessed letters
                ));
            // Add a row for displaying letters with their states
            table.AddRow(new Markup(lettersDisplay));

            table.AddEmptyRow();

                // Style the table
                table.Border(TableBorder.Rounded).BorderColor(Color.Blue);
                table.LeftAligned();
                table.Collapse();

                return table;
            }

        //Purpose: Displays all of the stats for the day in a table
        //Parameters: string message
        //Returns: Table
        static Table DisplayStats(string message)
            {
            var lines = message.Split('\n');
            var statsTable = new Table().Border(TableBorder.Rounded);
            statsTable.AddColumn("Game Statistics");

            foreach (var line in lines)
                {
                    if (line.StartsWith("Players:"))
                    {
                    statsTable.AddRow("Players: " + line.Substring(8).Trim());
                    }
                    else if (line.StartsWith("Winners:"))
                    {
                    statsTable.AddRow("Win Percentage: " + line.Substring(8).Trim());
                    }
                    else if (line.StartsWith("Guess Distribution:"))
                    {
                    statsTable.AddEmptyRow();

                    statsTable.AddRow("Guess Distribution...");
                        var distribution = line.Substring(19).Trim();
                        var list = distribution.Split(',');

                        int count = 1;
                        foreach (var item in list)
                        {
                        statsTable.AddRow("" + count + ": " + item);
                            count++;
                        }
                    }
                }
            // Style the table
            statsTable.Border(TableBorder.Rounded).BorderColor(Color.Blue);
            statsTable.LeftAligned();
            statsTable.Collapse();

            // Render the table to the console
            return statsTable;
        }
    }
}