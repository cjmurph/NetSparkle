using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace NetSparkleWPF.ViewModel
{
    public abstract class ObservableObject : INotifyPropertyChanged, IDataErrorInfo
    {
        public object ValidationContext { get; set; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private bool _isExpanded;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// This is required to create on property changed events
        /// </summary>
        /// <param name="name">What property of this object has changed</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if (Validators.ContainsKey(name))
                UpdateError();
        }

        #endregion

        #region Data Validation

        private Dictionary<string, object> PropertyGetters
        {
            get
            {
                return GetType().GetProperties().Where(p => GetValidations(p).Length != 0).ToDictionary(p => p.Name, GetValueGetter);
            }
        }

        private Dictionary<string, ValidationAttribute[]> Validators
        {
            get
            {
                return GetType().GetProperties().Where(p => GetValidations(p).Length != 0).ToDictionary(p => p.Name, GetValidations);
            }
        }

        private static ValidationAttribute[] GetValidations(PropertyInfo property) => (ValidationAttribute[])property.GetCustomAttributes(typeof(ValidationAttribute), true);

        private object GetValueGetter(PropertyInfo property)
        {
            return property.GetValue(this, null);
        }

        public string Error { get; private set; }

        private void UpdateError()
        {
            var errors = from i in Validators
                         from v in i.Value
                         where !Validate(v, PropertyGetters[i.Key])
                         select v.ErrorMessage;
            Error = string.Join(Environment.NewLine, errors.ToArray());
            OnPropertyChanged(nameof(Error));
        }

        public string this[string columnName]
        {
            get
            {
                if (PropertyGetters.ContainsKey(columnName))
                {
                    try
                    {
                        var value = PropertyGetters[columnName];
                        var errors = Validators[columnName].Where(v => !Validate(v, value))
                            .Select(v => v.ErrorMessage).ToArray();

                        OnPropertyChanged(nameof(Error));
                        return string.Join(Environment.NewLine, errors);
                    }
                    catch { }
                }

                OnPropertyChanged(nameof(Error));
                return string.Empty;
            }
        }

        private bool Validate(ValidationAttribute v, object value)
        {
            if (ValidationContext != null)
                return v.GetValidationResult(value, new ValidationContext(ValidationContext)) == ValidationResult.Success;
            return false;
        }

        #endregion
    }
}

