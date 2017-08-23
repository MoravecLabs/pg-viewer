// <copyright file="Atom.cs" company="Moravec Labs, LLC">
//     MIT License
//
//     Copyright (c) Moravec Labs, LLC.
//
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.
// </copyright>

namespace MoravecLabs.Atom
{
    using System;
    using System.Collections.Generic;
    using Infrastructure;
    using System.Threading.Tasks;

    /// <summary>
    /// A simple event driven wrapper class that can wrap any c# type and provide events on change.
    /// If appropriate, these events can be connected to a ViewModel NotifyPropertyChanged as well.
    /// </summary>
    /// <typeparam name="T">The type of the value that the atom wraps</typeparam>
    public class Atom<T> : AtomCore
    {
        /// <summary>
        /// A reference to the viewModel, if the atom is attached to one.
        /// </summary>
        private BaseViewModel viewModel;

        /// <summary>
        /// The name of the property for this atom in the viewModel, if one is attached.
        /// </summary>
        private string viewModelPropertyName;

        /// <summary>
        /// The actual value tracked by this atom.
        /// </summary>
        private T internalValue;

        /// <summary>
        /// A list of actions that are called when the value changes.
        /// </summary>
        private List<Action<T, T>> onChangeActions;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MoravecLabs.Atom.Atom`1"/> class.
        /// </summary>
        public Atom()
        {
            this.internalValue = default(T);
            this.onChangeActions = new List<Action<T, T>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MoravecLabs.Atom.Atom`1"/> class.
        /// </summary>
        /// <param name="value">The initial value</param>
        public Atom(T value)
        {
            this.internalValue = value;
            this.onChangeActions = new List<Action<T, T>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MoravecLabs.Atom.Atom`1"/> class.
        /// </summary>
        /// <param name="value">The initial value</param>
        /// <param name="viewModel">The view model that this property belongs to</param>
        /// <param name="propertyName">The property name in the view model this belongs to</param>
        public Atom(T value, BaseViewModel viewModel, string propertyName)
        {
            this.internalValue = value;
            this.viewModel = viewModel;
            this.viewModelPropertyName = propertyName;
            this.onChangeActions = new List<Action<T, T>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MoravecLabs.Atom.Atom`1"/> class.
        /// </summary>
        /// <param name="value">The initial value</param>
        /// <param name="viewModel">The view model that this property belongs to</param>
        /// <param name="propertyName">The property name in the view model this belongs to</param>
        /// <param name="valueFunction">Returns a new value for this atom.  Called whenever any atom in atomsToMonitor change.</param>
        /// <param name="atomsToMonitor">A list of atoms to monitor, if any chane, valueFunction is called.</param>
        public Atom(T value, BaseViewModel viewModel, string propertyName, Func<IEnumerable<AtomCore>, T> valueFunction, IEnumerable<AtomCore> atomsToMonitor):this(value, viewModel, propertyName)
        {
            foreach(var a in atomsToMonitor)
            {
                a.AtomChanged += (o, n) => { this.Value = valueFunction(atomsToMonitor); };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:MoravecLabs.Atom.Atom`1"/> class.
        /// </summary>
        /// <param name="value">The initial value</param>
        /// <param name="viewModel">The view model that this property belongs to</param>
        /// <param name="propertyName">The property name in the view model this belongs to</param>
        /// <param name="valueFunction">Returns a new value for this atom.  Called whenever any atom in atomsToMonitor change.</param>
        /// <param name="atomsToMonitor">A list of atoms to monitor, if any chane, valueFunction is called.</param>
        public Atom(T value, BaseViewModel viewModel, string propertyName, Func<IEnumerable<AtomCore>, Task<T>> valueFunction, IEnumerable<AtomCore> atomsToMonitor) : this(value, viewModel, propertyName)
        {
            foreach (var a in atomsToMonitor)
            {
                a.AtomChanged += async (o, n) => { this.Value = await valueFunction(atomsToMonitor); };
            }
        }

        /// <summary>
        /// Gets or sets the value of the wrapped entity.
        /// </summary>
        public T Value
        {
            get { return this.Get(); }
            set { this.Set(value); }
        }

        /// <summary>
        /// Set the specified value.  The value must be different from the current value to be set.
        /// </summary>
        /// <param name="value">The value to set</param>
        /// <returns>True if the value was set, false if the value is the same as the existing value</returns>
        public bool Set(T value)
        {
            if (Equals(value, this.internalValue))
            {
                return false;
            }

            var oldValue = this.internalValue;
            this.internalValue = value;

            // process events here.
            if (this.viewModel != null)
            {
                this.viewModel.NotifyPropertyChanged(this.viewModelPropertyName);
            }

            this.onChangeActions.ForEach(a => a(oldValue, this.internalValue));
            this.AtomChangedEvent(oldValue, this.internalValue);

            return true;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        /// <returns>The current value</returns>
        public T Get()
        {
            return this.internalValue;
        }

        public void NotifyPropertyChanged()
        {
            // process events here.
            if (this.viewModel != null)
            {
                this.viewModel.NotifyPropertyChanged(this.viewModelPropertyName);
            }

            this.onChangeActions.ForEach(a => a(this.internalValue, this.internalValue));
            this.AtomChangedEvent(this.internalValue, this.internalValue);
        }

        /// <summary>
        /// Adds the callback to the subscription list.  The callback is called whenever the value is set.  The value must
        /// be different in order to be set.
        /// </summary>
        /// <param name="callBack">Call back.</param>
        public void SubscribePropertyChanged(Action<T, T> callBack)
        {
            this.onChangeActions.Add(callBack);
        }
    }
}
