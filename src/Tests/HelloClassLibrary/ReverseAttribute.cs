using System;
using System.Text;
using PostSharp.Serialization;

namespace HelloClassLibrary
{
    [PSerializable]
    public sealed class ReverseAttribute : FilterAttribute
    {
        public override object ApplyFilter(object value)
        {
            if (value == null)
                return null;

            string s = (string) value;

            StringBuilder stringBuilder = new StringBuilder(s.Length);
            for (int i = s.Length - 1; i >= 0; i--)
            {
                stringBuilder.Append(s[i]);
            }

            string newValue = stringBuilder.ToString();

            Console.WriteLine(">>> Encrypting value {0} to {1}", s, newValue );

            return newValue;

        }
    }
}