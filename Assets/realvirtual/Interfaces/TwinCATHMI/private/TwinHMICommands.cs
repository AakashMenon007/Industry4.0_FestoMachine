using System;

namespace realvirtual
{
    // TwinCAT Commands (Non Stnadard Part of a Message)
    [Serializable]
    public class TwinCATCommandSubscribe
    {
        public string[] commandOptions;
        public string symbol;

        public TwinCATCommandSubscribe(string symbol, bool poll)
        {
            string[] commandoptions;
            if (poll == true)
                commandoptions = new[] {"SendErrorMessage", "Poll"};
            else
                commandoptions = new[] {"SendErrorMessage"};
            this.commandOptions = commandoptions;
            this.symbol = symbol;
        }
    }

    [Serializable]
    public class TwinCATCommandWriteFloat
    {

        public string[] commandOptions;
        public string symbol;
        public float writeValue;
        public TwinCATCommandWriteFloat(string symbol, float value)
        {
            commandOptions = new string[] {"SendErrorMessage", "SendWriteValue"};
            this.symbol = symbol;
            this.writeValue = value;
        }
    }
    
    [Serializable]
    public class TwinCATCommandReadSmybol
    {
        public string[] commandOptions;
        public string symbol;
        public TwinCATCommandReadSmybol(string symbol)
        {
            commandOptions = new string[] {"SendErrorMessage", "SendWriteValue"};
            this.symbol = symbol;
        }
    }

    [Serializable]
    public class TwinCATCommandWriteInt
    {
        public string[] commandOptions;
        public string symbol;
        public float writeValue;
        public TwinCATCommandWriteInt(string symbol, int value)
        {
            commandOptions = new string[] {"SendErrorMessage", "SendWriteValue"};
            this.symbol = symbol;
            this.writeValue = value;
        }
    }
    
    [Serializable]
    public class TwinCATCommandWriteBool
    {
        public string[] commandOptions;
        public string symbol;
        public bool writeValue;
        public TwinCATCommandWriteBool(string symbol, bool value)
        {
            commandOptions = new string[] {"SendErrorMessage", "SendWriteValue"};
            this.symbol = symbol;
            this.writeValue = value;
        }
    }
    
    [Serializable]
    public class TwinCATCommandWriteText
    {
        public string[] commandOptions;
        public string symbol;
        public string writeValue;
        public TwinCATCommandWriteText(string symbol, string value)
        {
            commandOptions = new string[] {"SendErrorMessage", "SendWriteValue"};
            this.symbol = symbol;
            this.writeValue = value;
        }
    }

    // TwinCAT Messages - which are send over Websocked - Commands are a part of a message
    [Serializable]
    public class TwinCATSubscribe
    {
        public int id;
        public int intervalTime;
        public string requestType;
        public TwinCATCommandSubscribe[] commands;

        public TwinCATSubscribe(string[] symbols, int theid, int intervalltime, bool poll)
        {
            requestType = "Subscription";
            id = theid;
            intervalTime = intervalltime;
       
            commands = new TwinCATCommandSubscribe[symbols.Length];
            for (int i = 0; i < symbols.Length; i++)
            {
                TwinCATCommandSubscribe commandSubscribe = new TwinCATCommandSubscribe(symbols[i], poll);
                commands[i] = commandSubscribe;
            }
        }
    }

    [Serializable]
    public class TwinCATReadSymbol
    {
        public string requestType;
        public TwinCATCommandReadSmybol[] commands;
        public int id;
        public TwinCATReadSymbol(string symbol)
        {
            requestType = "ReadWrite";
            string[] commandoptions = new[] {"SendErrorMessage", "SendWriteValue"};
            var command = new TwinCATCommandReadSmybol(symbol);
            commands = new TwinCATCommandReadSmybol[1];
            commands[0] = command;
        }
    }
    
    [Serializable]
    public class TwinCATWriteFloat
    {
        public string requestType;
        public TwinCATCommandWriteFloat[] commands;
        public int id;
        public TwinCATWriteFloat(string symbol, float value)
        {
            requestType = "ReadWrite";
            string[] commandoptions = new[] {"SendErrorMessage", "SendWriteValue"};
            var command = new TwinCATCommandWriteFloat(symbol, value);
            commands = new TwinCATCommandWriteFloat[1];
            commands[0] = command;
        }
    }

    [Serializable]
    public class TwinCATWriteInt
    {
        public string requestType;
        public TwinCATCommandWriteInt[] commands;
        public int id;
        public TwinCATWriteInt(string symbol, int value)
        {
            requestType = "ReadWrite";
            string[] commandoptions = new[] {"SendErrorMessage", "SendWriteValue"};
            var command = new TwinCATCommandWriteInt(symbol, value);
            commands = new TwinCATCommandWriteInt[1];
            commands[0] = command;
        }
    }

    [Serializable]
    public class TwinCATWriteBool
    {
        public string requestType;
        public TwinCATCommandWriteBool[] commands;
        public int id;
        public TwinCATWriteBool(string symbol,  bool value)
        {
            requestType = "ReadWrite";
            string[] commandoptions = new[] {"SendErrorMessage", "SendWriteValue"};
            var command = new TwinCATCommandWriteBool(symbol, value);
            commands = new TwinCATCommandWriteBool[1];
            commands[0] = command;
        }
    }
    
    [Serializable]
    public class TwinCATWriteText
    {
        public string requestType;
        public TwinCATCommandWriteText[] commands;
        public int id;
        public TwinCATWriteText(string symbol,  string value)
        {
            requestType = "ReadWrite";
            string[] commandoptions = new[] {"SendErrorMessage", "SendWriteValue"};
            var command = new TwinCATCommandWriteText(symbol, value);
            commands = new TwinCATCommandWriteText[1];
            commands[0] = command;
        }
    }

    [Serializable]
    public class TwinCATError
    {
        public string domain;
        public int code;
        public string message;
        public string reason;
    }
    
}