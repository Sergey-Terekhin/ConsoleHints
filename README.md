# ConsoleHints
Small library for autocomplete hints in the console applications

## Usage
```C#
var commands = new[]
{
  "dir",
  "help",
  "copy",
  "delete"
};
var inputReader = new ConsoleHintedInput(commands.OrderBy(s => s));
var commandLine = inputReader.ReadHintedLine();

//do something with user command
```

## Features

1. Autocomplete hints with highlighting of inputed charachters
2. Commands history

Following hot keys are supported out-of-box:
* `Enter` ends read loop and returns user's input
* `Backspace` removes last charachter from user's input
* `Tab` completes user's input with active hint (if found)
* `Space` completes user's input (if found) or adds space character to the end of line
* `Up arrow` select previous command from history if user's input is empty or select previous hint (if possible)
* `Down arrow` select next command from history if user's input is empty or select next hint (if possible)

Modify internal `switch` in the `ReadHintedLine` function to add processing of other keys

![Demo](https://github.com/Sergey-Terekhin/ConsoleHints/blob/master/demo.gif)
