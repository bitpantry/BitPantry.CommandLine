using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BitPantry.CommandLine.Interface
{
    /// <summary>
    /// The basis of the intercept for the standard console outputs - this class provides all standard text writer
    /// functions but allows for an intercept and manipulation before finally writing to the final destination.
    /// </summary>
    public abstract class Writer : StringWriter
    {
        public override void Write(char value) { WriteGeneric<char>(value); }
        public override void Write(string value) { WriteGeneric<string>(value); }
        public override void Write(bool value) { WriteGeneric<bool>(value); }
        public override void Write(int value) { WriteGeneric<int>(value); }
        public override void Write(double value) { WriteGeneric<double>(value); }
        public override void Write(long value) { WriteGeneric<long>(value); }
        public override void Write(string format, params object[] args) { WriteGeneric<string>(string.Format(format, args)); }

        public override void WriteLine(char value) { WriteLineGeneric<char>(value); }
        public override void WriteLine(string value) { WriteLineGeneric<string>(value); }
        public override void WriteLine(bool value) { WriteLineGeneric<bool>(value); }
        public override void WriteLine(int value) { WriteLineGeneric<int>(value); }
        public override void WriteLine(double value) { WriteLineGeneric<double>(value); }
        public override void WriteLine(long value) { WriteLineGeneric<long>(value); }
        public override void WriteLine(string format, params object[] arg) { WriteLineGeneric<string>(string.Format(format, arg)); }

        private void WriteGeneric<T>(T value) { OnWrite(value.ToString()); }
        private void WriteLineGeneric<T>(T value) { OnWrite(string.Format("{0}{1}", value == null ? null : value.ToString(), base.NewLine)); }

        public override void Write(char[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
            char[] buffer2 = new char[count]; //Ensures large buffers are not a problem
            for (int i = 0; i < count; i++) buffer2[i] = buffer[index + i];
            WriteGeneric<string>(CharArrToString(buffer2));
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
            char[] buffer2 = new char[count]; //Ensures large buffers are not a problem
            for (int i = 0; i < count; i++) buffer2[i] = buffer[index + i];
            WriteLineGeneric<string>(CharArrToString(buffer2));
        }

        private string CharArrToString(char[] arr)
        {
            StringBuilder strBldr = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
                strBldr.Append(arr[i]);
            return strBldr.ToString();
        }

        /// <summary>
        /// All write output functions funnel into this function for interception
        /// </summary>
        /// <param name="str">The string representation of data written to the output</param>
        protected abstract void OnWrite(string str);

    }
}
