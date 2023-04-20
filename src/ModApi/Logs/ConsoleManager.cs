using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModLoader.Events;

namespace ModLoader.Logs
{
    internal class ConsoleManager : IConsoleHelper
    {
        private ModHelper helper;
        private Dictionary<string, Action<string, string[]>> consoleCommands;
        private Action<object, ConsoleInputReceivedEventArgs> commandHandler;
        private bool ingoreConsole = false;

        private static bool _shouldWriteToConsole = false;
        public static bool WriteToConsole
        {
            get
            {
                return _shouldWriteToConsole || ModApi.Singleton.config.Verbose;
            }
            set
            {
                _shouldWriteToConsole = value;
            }
        }

        public ConsoleManager(ModHelper modhelper)
        {
            ingoreConsole = File.Exists("Mono.Posix.dll");

            helper = modhelper;
            consoleCommands = new Dictionary<string, Action<string, string[]>>();
        }


        public void Log(string message) => WriteMessage(message, "LOG", ConsoleColor.DarkGray);
        public void Trace(string message) => WriteMessage(message, "TRACE", ConsoleColor.DarkGray);
        public void Debug(string message) => WriteMessage(message, "DEBUG", ConsoleColor.DarkMagenta);
        public void Info(string message) => WriteMessage(message, "INFO", ConsoleColor.White);
        public void Error(string message) => WriteMessage(message, "ERROR", ConsoleColor.Red);
        public void Announcement(string message) => WriteMessage(message, "INFO", ConsoleColor.DarkGreen);
        public void Warn(string message) => WriteMessage(message, "WARN", ConsoleColor.DarkYellow);
        public void Success(string message, string info) => WriteTwoPartMessage(message, info, "INFO", ConsoleColor.White, ConsoleColor.Green);
        public void Failure(string message, string info) => WriteTwoPartMessage(message, info, "WARN", ConsoleColor.White, ConsoleColor.Red);
        public void Log(string message, string info) => WriteTwoPartMessage(message, info, "INFO", ConsoleColor.White, ConsoleColor.White);

        internal void WriteMessage(string message, string type, ConsoleColor color)
        {
            if (ingoreConsole)
                return;

            WriteToConsole = true;

            Console.ForegroundColor = color;
            Console.WriteLine($"[{helper.Manifest.Name}][{type}] {message}");
            Console.Out.Flush();
            Console.ForegroundColor = ConsoleColor.White;

            WriteToConsole = false;
        }

        internal void WriteTwoPartMessage(string message1, string message2, string type, ConsoleColor color1, ConsoleColor color2)
        {
            if (ingoreConsole)
                return;

            WriteToConsole = true;

            Console.ForegroundColor = color1;
            Console.Write($"[{helper.Manifest.Name}][{type}] {message1}");
            Console.ForegroundColor = color2;
            Console.Write($"\t{message2}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            WriteToConsole = false;
        }

        public void AddConsoleCommand(string trigger, Action<string, string[]> handler)
        {
            if (consoleCommands.ContainsKey(trigger.ToLower()))
                return;

            consoleCommands.Add(trigger.ToLower(), handler);
            if(commandHandler == null)
            {
                commandHandler = (sender, args) =>
                {
                    string[] inputs = args.Input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (inputs.Length > 0 && inputs[0].ToLower() is string key && consoleCommands.ContainsKey(key) && consoleCommands[key] is Action<string, string[]> action)
                        action.Invoke(inputs[0], inputs.Length > 1 ? inputs.Skip(1).ToArray() : new string[0]);
                };

                EventManager.Singleton.ConsoleInputReceived += (sender, args) => commandHandler(sender, args);
            }
        }

    }
}