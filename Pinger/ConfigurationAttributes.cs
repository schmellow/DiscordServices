using System;
using System.Collections.Generic;
using System.Text;

namespace Schmellow.DiscordServices.Pinger
{
    public abstract class GuildPropertyAttribute : Attribute
    {
        public string Description { get; private set; }

        public GuildPropertyAttribute(string description)
        {
            Description = description;
        }

        public abstract object ParseValues(params string[] values);
    }

    public sealed class StringPropertyAttribute : GuildPropertyAttribute
    {
        public StringPropertyAttribute(string description) : base(description)
        {
        }

        public override object ParseValues(params string[] values)
        {
            return string.Join("", values);
        }
    }

    public sealed class SingleStringPropertyAttribute : GuildPropertyAttribute
    {
        public SingleStringPropertyAttribute(string description) : base(description)
        {
        }

        public override object ParseValues(params string[] values)
        {
            if (values.Length == 0)
                return string.Empty;
            if (values.Length > 1)
                throw new ArgumentException("Expected single value");
            return values[0];
        }
    }

    public sealed class StringSetPropertyAttribute : GuildPropertyAttribute
    {
        public StringSetPropertyAttribute(string description) : base(description)
        {
        }

        public override object ParseValues(params string[] values)
        {
            return new HashSet<string>(values);
        }
    }

    public sealed class IntegerPropertyAttribute : GuildPropertyAttribute
    {
        public IntegerPropertyAttribute(string description) : base(description)
        {
        }

        public override object ParseValues(params string[] values)
        {
            if(values.Length == 1)
            {
                int value;
                if (int.TryParse(values[0], out value))
                    return value;
            }
            else if (values.Length > 1)
                throw new ArgumentException("Expected single value");
            return 0;
        }
    }

    public sealed class BooleanPropertyAttribute : GuildPropertyAttribute
    {
        public BooleanPropertyAttribute(string description) : base(description)
        {
        }

        public override object ParseValues(params string[] values)
        {
            if (values.Length == 1)
            {
                bool value;
                if (bool.TryParse(values[0], out value))
                    return value;
            }
            else if (values.Length > 1)
                throw new ArgumentException("Expected single value");
            return false;
        }
    }
}
