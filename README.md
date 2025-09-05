# C# Cowsay Wrapper

A simple C# program that demonstrates core .NET concepts through a fun cowsay wrapper.

## Prerequisites

- .NET 6.0 or later
- `cowsay` installed on your system

### Installing Cowsay

**macOS (using Homebrew):**

```bash
brew install cowsay
```

**Ubuntu/Debian:**

```bash
sudo apt-get install cowsay
```

## How to Run

1. Clone this repository
2. Navigate to the project directory
3. Run the program:

```bash
dotnet run
```

4. Enter your message when prompted
5. Enjoy your talking cow! 🐄

## Key Learning Points

1. **C# as System Language**: Despite being high-level, C# can effectively manage system processes
2. **Process Management**: The `System.Diagnostics.Process` class provides powerful process control
3. **STDIO Streams**: Understanding how programs communicate through standard input/output
4. **Cross-Platform**: This same code works on Windows, macOS, and Linux with .NET

## Technical Concepts Explained

- **Child Process**: The cowsay program runs as a separate process created by our C# program
- **Standard Output**: We capture the text output that cowsay would normally print to the terminal
- **Process Lifecycle**: Start → Execute → Wait → Read Output → Clean Up

## Example Output

```
Enter message: Hello from C#!
 _________________
< Hello from C#! >
 -----------------
        \   ^__^
         \  (oo)\_______
            (__)\       )\/\
                ||----w |
                ||     ||
```
