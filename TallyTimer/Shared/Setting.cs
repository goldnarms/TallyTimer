// Copyright (c) Adam Nathan.  All rights reserved.
//
// By purchasing the book that this source code belongs to, you may use and
// modify this code for commercial and non-commercial applications, but you
// may not publish the source code.
// THE SOURCE CODE IS PROVIDED "AS IS", WITH NO WARRANTIES OR INDEMNITIES.

using System.IO.IsolatedStorage;

namespace TallyTimer.Shared
{
    // Encapsulates a key/value pair stored in Isolated Storage ApplicationSettings
    public class Setting<T>
    {
        readonly string _name;
        T _value;
        readonly T _defaultValue;
        bool _hasValue;

        public Setting(string name, T defaultValue)
        {
            this._name = name;
            this._defaultValue = defaultValue;
        }

        public T Value
        {
            get
            {
                // Check for the cached value
                if (!this._hasValue)
                {
                    // Try to get the value from Isolated Storage
                    if (!IsolatedStorageSettings.ApplicationSettings.TryGetValue(
                          this._name, out this._value))
                    {
                        // It hasn't been set yet
                        this._value = this._defaultValue;
                        IsolatedStorageSettings.ApplicationSettings[this._name] = this._value;
                    }
                    this._hasValue = true;
                }

                return this._value;
            }

            set
            {
                // Save the value to Isolated Storage
                IsolatedStorageSettings.ApplicationSettings[this._name] = value;
                this._value = value;
                this._hasValue = true;
            }
        }

        public T DefaultValue
        {
            get { return this._defaultValue; }
        }

        // "Clear" cached value:
        public void ForceRefresh()
        {
            this._hasValue = false;
        }
    }
}