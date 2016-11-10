using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AS.Concept_vSdk.Cli
{
    public class ConsoleHintedInput
    {
        private class Suggestion
        {
            public string Value { get; set; }
            public int[] HighlightIndexes { get; set; }
        }

        private readonly IReadOnlyCollection<string> _hintsSource;
        private readonly List<string> _commandsHistory = new List<string>();
        private List<Suggestion> _suggestionsForUserInput;
        private int _suggestionPosition;
        private int _historyPosition;

        /// <summary>
        /// Creates new instance of <see cref="ConsoleHintedInput"/> class
        /// </summary>
        /// <param name="hintsSource">Collection containing input hints</param>
        public ConsoleHintedInput(IEnumerable<string> hintsSource)
        {
            _hintsSource = hintsSource.ToArray();
        }

        /// <summary>
        /// Reads input from user using hints. Commands history is supported
        /// </summary>
        /// <param name="inputRegex"></param>
        /// <param name="hintColor"></param>
        /// <returns></returns>
        public string ReadHintedLine(string inputRegex = ".*", ConsoleColor hintColor = ConsoleColor.DarkGray)
        {
            ConsoleKeyInfo input;

            Suggestion suggestion = null;
            var userInput = string.Empty;
            var readLine = string.Empty;

            while (ConsoleKey.Enter != (input = Console.ReadKey()).Key)
            {
                switch (input.Key)
                {
                    case ConsoleKey.Backspace:
                        userInput = userInput.Any() ? userInput.Remove(userInput.Length - 1, 1) : string.Empty;
                        break;
                    case ConsoleKey.Tab:
                        if (suggestion != null)
                        {
                            userInput = suggestion.Value + ' ';
                        }
                        break;
                    case ConsoleKey.Spacebar:
                        if (suggestion != null)
                            userInput = suggestion.Value + ' ';
                        else if (Regex.IsMatch(input.KeyChar.ToString(), inputRegex))
                        {
                            userInput += input.KeyChar;
                            UpdateSuggestionsForUserInput(userInput);
                            suggestion = GetFirstSuggestion();
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        if (string.IsNullOrWhiteSpace(userInput))
                        {
                            userInput = GetPreviousCommandFromHistory();
                        }
                        else
                        {
                            suggestion = GetPreviousSuggestion();
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (string.IsNullOrWhiteSpace(userInput))
                        {
                            userInput = GetNextCommandFromHistory();
                        }
                        else
                        {
                            suggestion = GetNextSuggestion();
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        break;
                    default:
                        if (Regex.IsMatch(input.KeyChar.ToString(), inputRegex))
                        {
                            userInput += input.KeyChar;
                        }
                        UpdateSuggestionsForUserInput(userInput);
                        suggestion = GetFirstSuggestion();
                        break;
                }


                readLine = suggestion != null ? suggestion.Value : userInput;

                ClearCurrentConsoleLine();

                Console.Write(userInput);
                if (userInput.Any())
                {
                    if (suggestion != null && suggestion.Value != userInput)
                    {
                        WriteSuggestion(suggestion, hintColor);
                    }
                }
            }
            ClearCurrentConsoleLine();
            Console.WriteLine(readLine);
            AddCommandToHistory(readLine);
            return readLine;
        }

        private static void WriteSuggestion(Suggestion suggestion, ConsoleColor hintColor)
        {
            if (suggestion.HighlightIndexes == null || !suggestion.HighlightIndexes.Any())
            {
                ConsoleUtils.Write($" ({suggestion.Value})", hintColor);
                return;
            }

            var orderedIndexes = suggestion.HighlightIndexes.OrderBy(v => v).ToArray();
            var idx = 0;
            var color = Console.ForegroundColor;

            ConsoleUtils.Write(" (", hintColor);
            for (var i = 0; i < suggestion.Value.Length; i++)
            {
                if (idx < orderedIndexes.Length && i == orderedIndexes[idx])
                {
                    idx++;
                    color = Console.ForegroundColor;
                }
                else
                {
                    color = hintColor;
                }
                ConsoleUtils.Write(suggestion.Value[i].ToString(), color);
            }
            ConsoleUtils.Write(")", hintColor);

        }

        private void UpdateSuggestionsForUserInput(string userInput)
        {
            _suggestionPosition = 0;

            if (string.IsNullOrEmpty(userInput))
            {
                _suggestionsForUserInput = null;
                return;
            }

            if (_hintsSource.All(item => item.Length < userInput.Length))
            {
                _suggestionsForUserInput = null;
                return;
            }

            //simple case then user's input is equal to start of hint
            var hints = _hintsSource
                .Where(item => item.Length > userInput.Length && item.Substring(0, userInput.Length) == userInput)
                .Select(hint => new Suggestion
                {
                    Value = hint,
                    HighlightIndexes = Enumerable.Range(0, userInput.Length).ToArray()
                })
                .ToList();

            //more complex case: tokenize hint and try to search user input from beginning of tokens
            foreach (var item in _hintsSource)
            {
                var parts = item.Split(new[] { ' ', ';', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

                var candidate = parts.FirstOrDefault(part => part.Length >= userInput.Length && part.Substring(0, userInput.Length) == userInput);
                if (candidate != null)
                {
                    hints.Add(new Suggestion
                    {
                        Value = item,
                        HighlightIndexes = Enumerable.Range(item.IndexOf(candidate), userInput.Length).ToArray()
                    });
                }
            }

            //try to split user's input into separate char and find all of them into string

            foreach (var item in _hintsSource)
            {
                var highlightIndexes = new List<int>();
                var startIndex = 0;
                var found = true;
                for (var i = 0; i < userInput.Length; i++)
                {
                    if (startIndex >= item.Length)
                    {
                        found = false;
                        break;
                    }

                    var substring = item.Substring(startIndex);
                    var idx = substring.IndexOf(userInput[i]);
                    if (idx < 0)
                    {
                        //no such symbol in the hints source item
                        found = false;
                        break;
                    }
                    startIndex = startIndex + idx + 1;
                    highlightIndexes.Add(startIndex - 1);
                }
                if (found)
                {
                    hints.Add(new Suggestion
                    {
                        Value = item,
                        HighlightIndexes = highlightIndexes.ToArray()
                    });
                }
            }

            _suggestionsForUserInput = hints;
        }
        
        private void AddCommandToHistory(string readLine)
        {
            _commandsHistory.Add(readLine);
            _historyPosition = _commandsHistory.Count;
        }

        private string GetNextCommandFromHistory()
        {
            if (!_commandsHistory.Any())
                return string.Empty;
            _historyPosition++;
            if (_historyPosition >= _commandsHistory.Count)
            {
                _historyPosition = _commandsHistory.Count - 1;
            }
            return _commandsHistory[_historyPosition];
        }

        private string GetPreviousCommandFromHistory()
        {
            if (!_commandsHistory.Any())
                return string.Empty;

            _historyPosition--;
            if (_historyPosition >= _commandsHistory.Count)
            {
                _historyPosition = _commandsHistory.Count - 1;
            }
            if (_historyPosition < 0)
            {
                _historyPosition = 0;
            }
            return _commandsHistory[_historyPosition];
        }

        private Suggestion GetFirstSuggestion()
        {
            return _suggestionsForUserInput?.FirstOrDefault();
        }

        private Suggestion GetNextSuggestion()
        {
            if (_suggestionsForUserInput == null || !_suggestionsForUserInput.Any())
                return null;

            _suggestionPosition++;
            if (_suggestionPosition >= _suggestionsForUserInput.Count)
            {
                _suggestionPosition = 0;
            }
            return _suggestionsForUserInput[_suggestionPosition];
        }

        private Suggestion GetPreviousSuggestion()
        {
            if (_suggestionsForUserInput == null || !_suggestionsForUserInput.Any())
                return null;

            _suggestionPosition--;
            if (_suggestionPosition < 0)
            {
                _suggestionPosition = _suggestionsForUserInput.Count-1;
            }
            return _suggestionsForUserInput[_suggestionPosition];
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
            ConsoleUtils.WritePrompt();
        }
    }
}
