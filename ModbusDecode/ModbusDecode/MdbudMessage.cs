﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModbusDecode
{
    class MdbusFloat
    {
        public float Value { get; set; }
        public string RawString { get; set; }
        public string FloatString { get; set; }

        // User-defined conversion from MdbusFloat to float 
        public static implicit operator float(MdbusFloat mf)
        {
            return mf.Value;
        }
        //  User-defined conversion from float to MdbusFloat 
        public static implicit operator MdbusFloat(float f)
        {
            return new MdbusFloat() { Value = f};
        }
    }

    class MdbusMessage
    {
        public int SlaveId { get; private set; }
        public int FunctionCode { get; private set; }
        public Nullable<int> StartAddress { get; private set; }
        public Nullable<int> RegisterCount { get; private set; }
        public int ByteCount { get; private set; }
        public string Checksum { get; set; }
        public List<MdbusFloat> Values { get; private set; }

        public static MdbusMessage Decode(string message)
        {
            return MdbusMessage.Decode(message, true);
        }

        /// <summary>
        /// Decodes a message string from Mdbus.exe (Calta Software)
        /// </summary>
        /// <param name="message">Message string from Mdbus Monitor logging, starting with the Slave ID</param>
        /// <param name="modiconFloat">True if Modicon Float is used (the least significant bytes are sent in the first register and the most significant bytes in the second register of a pair)</param>
        /// <example>
        /// 
        /// Example responses:
        /// 
        /// 3 (0x03) Read Holding Registers
        /// SlaveID=1, FC=0x03, ByteCount=0x10 (16), values: 60 3A 46 ....
        /// 01 03 10 60 3A 46 33 69 89 44 57 33 CE 43 06 8B 59 3B 72 C4 8E 
        /// 
        /// 4 (0x04) Read Input Registers
        /// SlaveID=1, FC=0x04, ByteCount=0x10 (16), values: 60 3A 46 ....
        /// 01 04 10 60 3A 46 33 69 89 44 57 33 CE 43 06 8B 59 3B 72 C4 8E 
        /// 
        /// 16 (0x10) Write Multiple Registers 
        /// SlaveID=1, FC=0x10 (16), StartAddr=0x0064 (100), Registers=0x0032 (50), ByteCount=0x64 (100), Values:48 9C 1C ...
        /// 01 10 00 64 00 32 64 48 9C 1C B6 48 94 27 C8 48 98 95 47 48 87 F7 BD 42 AC 07 2B 42 AE 57 91 42 AC 89 5E 42 AF DE 29 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 45 C9 F0 4D 45 CA 95 4D 45 C9 23 FE 45 C9 64 DF 42 0A 66 66 42 0C CC CD 42 13 33 33 42 11 33 33 42 9E CC CD 9D A4 
        /// </example>
        /// <returns></returns>
        public static MdbusMessage Decode(string message, bool modiconFloat)
        {
            if (!message.Contains(' '))
            {
                // TODO: create string array from text without spaces.
                throw new ArgumentException("Given message string does not contain spaces. Must use a valid string from Mdbus Monitor log");
            }
            string[] hexValuesSplit = message.Trim().Split(' ');

            MdbusMessage mdbusMessage = new MdbusMessage();
            mdbusMessage.Values = new List<MdbusFloat>();

            if (hexValuesSplit.Length > 1)
            {
                mdbusMessage.SlaveId = Convert.ToInt32(hexValuesSplit[0], 16);
            }
            if (hexValuesSplit.Length > 2)
            {
                mdbusMessage.FunctionCode = Convert.ToInt32(hexValuesSplit[1], 16);
            }

            int startByte;
            switch (mdbusMessage.FunctionCode)
            {
                case 3:
                case 4:
                    if (hexValuesSplit.Length > 3)
                    {
                        mdbusMessage.ByteCount = Convert.ToInt32(hexValuesSplit[2], 16);
                    }
                    startByte = 3;
                    break;
                case 16:
                    if (hexValuesSplit.Length > 4)
                    {
                        mdbusMessage.StartAddress = Convert.ToInt32(hexValuesSplit[2] + hexValuesSplit[3], 16);
                    }
                    if (hexValuesSplit.Length > 6)
                    {
                        mdbusMessage.RegisterCount = Convert.ToInt32(hexValuesSplit[4] + hexValuesSplit[5], 16);
                    }
                    if (hexValuesSplit.Length > 7)
                    {
                        mdbusMessage.ByteCount = Convert.ToInt32(hexValuesSplit[6], 16);
                    }
                    startByte = 7;
                    break;
                default:
                    startByte = 3;
                    break;
            }
            if (hexValuesSplit.Length > 1)
            {
                mdbusMessage.Checksum = hexValuesSplit[hexValuesSplit.Length - 2] + hexValuesSplit[hexValuesSplit.Length - 1];
            }
                    
            // convert all float values from hex string
            for (int i = startByte; (i - startByte < mdbusMessage.ByteCount) && (i < hexValuesSplit.Length - 3); i += 4)
            {
                MdbusFloat mdbusFloat = new MdbusFloat();
                mdbusFloat.RawString = hexValuesSplit[i] + ' ' + hexValuesSplit[i + 1] + ' ' + hexValuesSplit[i + 2] + ' ' + hexValuesSplit[i + 3];
                if (modiconFloat)
                {
                    mdbusFloat.FloatString = (hexValuesSplit[i + 2] + hexValuesSplit[i + 3] + hexValuesSplit[i + 0] + hexValuesSplit[i + 1]);
                }
                else
                {
                    mdbusFloat.FloatString = (hexValuesSplit[i + 0] + hexValuesSplit[i + 1] + hexValuesSplit[i + 2] + hexValuesSplit[i + 3]);
                }
                // Convert hex string to float value
                uint num = uint.Parse(mdbusFloat.FloatString, System.Globalization.NumberStyles.AllowHexSpecifier);

                byte[] floatVals = BitConverter.GetBytes(num);
                mdbusFloat.Value = BitConverter.ToSingle(floatVals, 0);
                
                mdbusMessage.Values.Add(mdbusFloat);
            }
            return mdbusMessage;
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendFormat("{0:D2} (0x{0:X2}) ", FunctionCode);
            switch (FunctionCode)
            {
                case 3:
                    strBuilder.AppendLine("Read Holding Registers");
                    break;
                case 4:
                    strBuilder.AppendLine("Read Input Registers");
                    break;
                case 8:
                    strBuilder.AppendLine("Diagnostic");
                    break;
                case 16:
                    strBuilder.AppendLine("Write Multiple Registers");
                    break;
                default:
                    strBuilder.AppendLine("Unknown Function Code");
                    break;
            }
            strBuilder.Append('-', 40).AppendLine();
            strBuilder.AppendLine(string.Format("{0,-20}{1,5} (0x{1:X2})", "Slave ID:", SlaveId));
            strBuilder.AppendLine(string.Format("{0,-20}{1,5} (0x{1:X2})", "Function Code:", FunctionCode));
            if (StartAddress.HasValue)
            {
                strBuilder.AppendLine(string.Format("{0,-20}{1,5} (0x{1:X4})", "Start Address:", StartAddress));
            }
            if (RegisterCount.HasValue)
            {
                strBuilder.AppendLine(string.Format("{0,-20}{1,5} (0x{1:X4})", "Register Count:", RegisterCount));
            }
            if (ByteCount > 0)
            {
                strBuilder.AppendLine(string.Format("{0,-20}{1,5} (0x{1:X2})", "Byte Count:", ByteCount));
            }
            strBuilder.AppendLine(string.Format("{0,-20}{1,5}", "Checksum:", Checksum));
            strBuilder.AppendFormat("Float Values ({0}):", Values.Count).AppendLine();
            var lineNumber = 1;
            foreach (var value in Values)
            {
                strBuilder.AppendLine(string.Format("{0,10:D3}: {1} -> {2} -> {3}", lineNumber++, value.RawString, value.FloatString, value.Value));
            }
            return strBuilder.ToString();
        }

    }
}
