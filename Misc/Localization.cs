using System.ComponentModel;

namespace System.Windows.Forms
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private bool _mInitialized;

        public LocalizedDescriptionAttribute(string key) : base(key)
        {
        }

        public override string Description
        {
            get
            {
                if (_mInitialized) return DescriptionValue;
                var key = base.Description;
                DescriptionValue = ResourceHelper.GetString(key) ?? string.Empty;

                _mInitialized = true;

                return DescriptionValue;
            }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class LocalizedCategoryAttribute : CategoryAttribute
    {
        public LocalizedCategoryAttribute(string key) : base(key)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            return ResourceHelper.GetString(value);
        }
    }
}
