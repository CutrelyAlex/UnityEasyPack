using System;
using System.Globalization;

namespace EasyPack.CustomData
{
    public class FloatValueHandler : IValueHandler
    {
        public CustomDataType SupportedType => CustomDataType.Float;

        public object GetValue(CustomDataEntry entry) => entry.FloatValue;

        public void SetValue(CustomDataEntry entry, object value)
        {
            entry.FloatValue = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            entry.Type = CustomDataType.Float;
            ClearOtherValues(entry);
        }

        public bool TryDeserialize(CustomDataEntry entry, string data, Type jsonClrType = null)
        {
            if (!float.TryParse(data, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture,
                    out float value))
            {
                return false;
            }

            entry.FloatValue = value;
            entry.Type = CustomDataType.Float;
            ClearOtherValues(entry);
            return true;
        }

        public string Serialize(CustomDataEntry entry) => entry.FloatValue.ToString("R", CultureInfo.InvariantCulture);

        public void Clear(CustomDataEntry entry)
        {
            entry.FloatValue = 0;
        }

        private static void ClearOtherValues(CustomDataEntry entry)
        {
            entry.IntValue = 0;
            entry.LongValue = 0;
            entry.BoolValue = false;
            entry.StringValue = null;
            entry.Vector2Value = default;
            entry.Vector3Value = default;
            entry.ColorValue = default;
            entry.JsonValue = null;
            entry.JsonClrType = null;
        }
    }
}