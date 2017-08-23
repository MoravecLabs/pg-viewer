// <copyright file="DelegateCommand.cs" company="Moravec Labs, LLC">
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

namespace MoravecLabs.Infrastructure
{
    using System;
    using System.Windows.Input;

    public class DelegateCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Action<T> executeMethod;
        private Func<T, bool> canExecuteMethod;
        private bool? lastCanExecute = null;

        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        public bool CanExecute(object parameter)
        {
            var canExe = this.canExecuteMethod((T)parameter);
            if (lastCanExecute != canExe)
            {
                this.lastCanExecute = canExe;
                this.CanExecuteChanged.Invoke(this, new EventArgs());
            }
            return canExe;
        }

        public void Execute(object parameter)
        {
            this.executeMethod((T)parameter);
        }
    }

    public class DelegateCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Action executeMethod;
        private Func<bool> canExecuteMethod;
        private bool? lastCanExecute = null;

        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        public bool CanExecute(object parameter)
        {
            var canExe = this.canExecuteMethod();
            if (lastCanExecute != canExe)
            {
                this.lastCanExecute = canExe;
                this.CanExecuteChanged?.Invoke(this, new EventArgs());
            }
            return canExe;
        }

        public void Execute(object parameter)
        {
            this.executeMethod();
        }
    }
}
