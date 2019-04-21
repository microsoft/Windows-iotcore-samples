//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using GPIOFruit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;


namespace GPIOFruit
{
    public static class Extensions
    {
        public static bool Update<T>(this ref T? val, T? newVal) where T : struct, IComparable<T>
        {
            Log.WriteLine("this {0} newVal {1}", val.HasValue ? val.ToString() : "(null)", newVal.HasValue ? newVal.ToString() : "(null)");
            if (newVal.HasValue && (!val.HasValue || newVal.Value.CompareTo(val.Value) != 0))
            {
                val = newVal;
                Log.WriteLine("updated {0}", val.HasValue ? val.ToString() : "(null)");
                return true;
            }
            return false;
        }
        public static GpioPinValue Invert(this GpioPinValue v)
        {
            return v == GpioPinValue.Low ? GpioPinValue.High : GpioPinValue.Low;
        }
        public static string ToStateString(this GpioPinValue v)
        {
            return v == GPIOBasePin.LED_OFF ? "Off" : "On";
        }
    }

    public abstract class GPIOBasePin : IDisposable
    {
        public const GpioPinValue LED_ON = GpioPinValue.Low;
        public const GpioPinValue LED_OFF = GpioPinValue.High;
        private GPIODevice _device;
        public GpioPinDriveMode Mode { get { return Pin.GetDriveMode(); } }
        public GpioPin Pin { get; private set; }
        public int Index { get { return Pin.PinNumber; }  }
        public GPIOBasePin(GPIODevice device, int index, GpioPinDriveMode mode)
        {
            Log.WriteLine("creating new base pin for idx {0}.  mode {1} device {2} null", index, mode, device == null ? "is" : "is not");

            _device = device;
            var controller = device.Device;
            Pin = controller.OpenPin(index);
            Pin.SetDriveMode(mode);
        }
        ~GPIOBasePin()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Pin != null)
                {
                    Pin.Dispose();
                    Pin = null;
                }
            }
        }
    }

    public class GPIOOutputPin : GPIOBasePin
    {
        public GpioPinValue _v;
        public GpioPinValue Value
        {
            get
            {
                return _v;
            }
            set
            {
                _v = value;
                Log.WriteLine("setting output pin {0} to {1}", Index, _v.ToString());
                Pin.Write(_v);
            }
        }
        public GPIOOutputPin(GPIODevice device, int index, GpioPinValue value, GpioPinDriveMode mode = GpioPinDriveMode.Output) : base(device, index, mode)
        {
            switch (mode)
            {
                case GpioPinDriveMode.Input:
                case GpioPinDriveMode.InputPullDown:
                case GpioPinDriveMode.InputPullUp:
                    throw new ArgumentException("output pins can't have input modes");
                default:
                    break;
            }
            Value = value;
        }
        public void Toggle()
        {
            Value = Value.Invert();
        }
    }
    public class GPIOInputPin : GPIOBasePin
    {
        public GpioPinValue Value
        {
            get
            {
                return Pin.Read();
            }
        }
        public void OnPinChanged(Object sender, GpioPinValueChangedEventArgs args)
        {
            var p = (GpioPin)sender;
            if (p.PinNumber != Index)
            {
                throw new ArgumentException(string.Format("unexpected: pin change event from {0} received by {1}", p.PinNumber, Index));
            }
            Log.WriteLine("{0} GPIOInputPin.OnPinChanged pin {1} edge {2}", Environment.TickCount, Index, args.Edge);
            InputChanged.Invoke(this, args);
        }
        public GPIOInputPin(GPIODevice device, int index, GpioPinDriveMode mode = GpioPinDriveMode.Input) : base(device, index, mode)
        {
            switch (mode)
            {
                case GpioPinDriveMode.Output:
                case GpioPinDriveMode.OutputOpenDrain:
                case GpioPinDriveMode.OutputOpenDrainPullUp:
                case GpioPinDriveMode.OutputOpenSource:
                case GpioPinDriveMode.OutputOpenSourcePullDown:
                    throw new ArgumentException("input pins can't have output modes");
                default:
                    break;
            }
            //Pin.DebounceTimeout = TimeSpan.FromMilliseconds(100);
            Log.WriteLine("GPIOInputPin {0} registering for change event", Index);
            Pin.ValueChanged += OnPinChanged;
        }
        public event EventHandler<GpioPinValueChangedEventArgs> InputChanged;
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected new virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Log.WriteLine("GPIOInputPin {0} UNregistering for change event", Index);
                Pin.ValueChanged -= OnPinChanged;
                base.Dispose(disposing);
            }
        }
    }

    [JsonObject(MemberSerialization.Fields)]
    public struct GpioPinIndexesType
    {
        public int? Blue;
        public int? Green;
        public int? Red;
        public int? Yellow;
        public int? Input;
        public override string ToString() {
            return String.Format("Pins B {0} G {1} R {2} Y {3} I {4}",
                Blue.HasValue ? Blue.ToString() : "(null)",
                Green.HasValue ? Green.ToString() : "(null)",
                Red.HasValue ? Red.ToString() : "(null)",
                Yellow.HasValue ? Yellow.ToString() : "(null)",
                Input.HasValue? Input.ToString() : "(null)");
        }
        public bool Update(GpioPinIndexesType value)
        {
            Log.WriteLine("original {0}", this.ToString());
            Log.WriteLine("new {0}", value.ToString());
            bool changed = Blue.Update(value.Blue);
            changed = Green.Update(value.Green) || changed;
            changed = Red.Update(value.Red) || changed;
            changed = Yellow.Update(value.Yellow) || changed;
            changed = Input.Update(value.Input) || changed;
            if (changed)
            {
                Log.WriteLine("updated {0}", this.ToString());
            }
            return changed;
        }
    }

    public class GPIODevice : IDisposable
    {
        public GpioController Device { get; set; }

        Dictionary<string, GPIOInputPin> _inputPins;
        Dictionary<string, GPIOOutputPin> _outputPins;
        GpioPinValue _defaultOutputState = GPIOOutputPin.LED_OFF;
        // not same as other SPB, private ctor, public static cread to allow multiple GPIOController 
        // gpiocontroller class only has getdefault(). there is no getdeviceselector(friendlynamestring)
        public GPIODevice()
        {
            _activeValue = _defaultOutputState.Invert();
            _inputPins = new Dictionary<string, GPIOInputPin>();
            _outputPins = new Dictionary<string, GPIOOutputPin>();
            Device = GpioController.GetDefault();
            Log.WriteLine("GPIO Device ctor complete.  controller {0} null", Device == null ? "is" : "is not");
        }
        ~GPIODevice()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Device != null)
                {
                    //Device.Dispose();
                    Device = null;
                }
                if (_inputPins != null)
                {
                    foreach (var kvp in _inputPins)
                    {
                        kvp.Value.Dispose();
                    }
                    _inputPins = null;
                }
                if (_outputPins != null)
                {
                    foreach (var kvp in _outputPins)
                    {
                        kvp.Value.Dispose();
                    }
                    _outputPins = null;
                }
            }
        }
        public void AddPin<T>(string name, T p, ref Dictionary<string, T> pins)
        {
            lock (pins)
            {
                T cur = default(T);
                if (pins.TryGetValue(name, out cur))
                {
                    Log.WriteLine("removing {0} for replacement", name);
                    pins.Remove(name);
                }
                Log.WriteLine("adding {0} to pin collection", name);
                pins.Add(name, p);
            }
        }

        public void AddInputPin(string name, int index, GpioPinDriveMode mode = GpioPinDriveMode.Input)
        {
            var newpin = new GPIOInputPin(this, index, mode);
            Log.WriteLine("GPIODevice registering for input change events for '{0}' pin {1}", name, newpin.Index);
            newpin.InputChanged += OnInputChanged;
            AddPin(name, newpin, ref _inputPins);
        }
        public void RemoveInputPin(string name, ref Dictionary<string, GPIOInputPin> pins)
        {
            lock (pins)
            {
                GPIOInputPin p;
                if (pins.TryGetValue(name, out p))
                {
                    Log.WriteLine("GPIODevice UNregistering for input change events for '{0}' pin {1}", name, p.Index);
                    p.InputChanged -= OnInputChanged;
                    pins.Remove(name);
                    p.Dispose();
                }
            }
        }
        public void AddOutputPin(string name, int index, GpioPinValue value, GpioPinDriveMode mode = GpioPinDriveMode.Output)
        {
            Log.WriteLine("creating new output pin {0} {1} {2}", name, index, value);
            var p = new GPIOOutputPin(this, index, value, mode);
            AddPin(name, p, ref _outputPins);
        }

        void AddOutputPin(string name, int newVal)
        {
            Log.WriteLine("adding {0} with default state {1}", name, _defaultOutputState);
            AddOutputPin(name, newVal, _defaultOutputState);
        }
        void RemoveOutputPin(string name, ref Dictionary<string, GPIOOutputPin> pins)
        {
            lock (pins)
            {
                GPIOOutputPin p = null;
                if (pins.TryGetValue(name, out p))
                {
                    pins.Remove(name);
                    p.Dispose();
                }
            }
        }
        public void InitInputPins(AppOptions o)
        {
            var pin_info = new (string Name, int? Index)[] {
                ( "input", o.Input ),
            };

            foreach (var p in pin_info)
            {
                if (p.Index.HasValue)
                {
                    AddInputPin(p.Name, p.Index.Value, GpioPinDriveMode.InputPullDown);
                }
            }
            Log.WriteLine("pins initialized");
            return;
        }
        public void InitOutputPins(AppOptions o)
        {
            var pin_info = new (string Name, int? Index)[] {
                ( "red", o.Red ),
                ( "yellow", o.Yellow ),
                ( "green",o.Green ),
                ( "blue", o.Blue ),
            };

            foreach (var p in pin_info)
            {
                if (p.Index.HasValue)
                {
                    AddOutputPin(p.Name, p.Index.Value);
                }
            }
            Log.WriteLine("pins initialized");
            return;
        }
        void UpdateOutputPin(int? newVal, string name)
        {
            if (newVal.HasValue)
            {
                lock (_outputPins)
                {
                    if (!_outputPins.ContainsKey(name))
                    {
                        Log.WriteLine("updateoutputpin adding new output pin {0} at index {1}", name, newVal);
                        AddOutputPin(name, newVal.Value);
                        if (_activePin != null && _activePin == name)
                        {
                            Log.WriteLine("updateoutputpin new pin is active. setting to {0}", _activeValue);
                            _outputPins[_activePin].Value = _activeValue;
                        }
                    }
                    else
                    {
                        if (newVal.Value != _outputPins[name].Index)
                        {
                            Log.WriteLine("updateoutputpin changing output pin {0} from index {1} to {2}", name, _outputPins[name].Index, newVal);
                            if (_activePin != null && _activePin == name)
                            {
                                Log.WriteLine("updateoutputpin new pin is active. setting prev index to {0}", _activeValue.Invert());
                                _outputPins[_activePin].Value = _activeValue.Invert();
                            }
                            RemoveOutputPin(name, ref _outputPins);
                            AddOutputPin(name, newVal.Value);
                            if (_activePin != null && _activePin == name)
                            {
                                Log.WriteLine("updateoutputpin new pin is active. setting new index to {0}", _activeValue.Invert());
                                _outputPins[_activePin].Value = _activeValue;
                            }
                        }

                    }
                }
            }
        }
        void UpdateInputPin(int? newVal, string name)
        {
            if (newVal.HasValue)
            {
                lock (_inputPins)
                {
                    if (!_inputPins.ContainsKey(name))
                    {
                        Log.WriteLine("updateinputpin adding new input pin {0} at index {1}", name, newVal);
                        AddInputPin(name, newVal.Value, GpioPinDriveMode.InputPullDown);
                    }
                    else
                    {
                        if (newVal.Value != _inputPins[name].Index)
                        {
                            Log.WriteLine("updateinputpin changing input pin {0} from index {1} to {2}", name, _outputPins[name].Index, newVal);
                            RemoveInputPin(name, ref _inputPins);
                            AddInputPin(name, newVal.Value, GpioPinDriveMode.InputPullDown);
                        }

                    }
                }
            }
        }
        public void OnInputChanged(Object sender, GpioPinValueChangedEventArgs args )
        {
            var pin = (GPIOInputPin)sender;
            Log.WriteLine("GPIODevice.OnInputChanged {0}", pin.Index);
            var v = args.Edge;
            if ( v == GpioPinEdge.RisingEdge)
            {
                Log.WriteLine("GPIODevice.OnInputChanged Blinking");
                Blink();// if it's still high treat as real
            }

        }
        public void LogInputPins()
        {
            lock (_inputPins) {
                foreach(var kv in _inputPins)
                {
                    Log.WriteLine("'{0}' pin {1} currently {2}", kv.Key, kv.Value.Index, kv.Value.Value);
                }
            }
        }
        int _blinkRate = 0;
        public void Blink()
        {
            lock (_inputPins)
            {
                bool fStart = false;
                if (_blinkRate == 0)
                {
                    fStart = true;
                }
                _blinkRate += 500;
                if (_blinkRate > 2000)
                {
                    _blinkRate = 0;
                }
                Log.WriteLine("blink rate {0}", _blinkRate);
                if (fStart)
                {
                    Task.Run(() =>
                    {
                        Log.WriteLine("starting blink task");
                        int delay = 0;
                        do
                        {
                            lock (_inputPins)
                            {
                                if (delay != _blinkRate)
                                {
                                    Log.WriteLine("blink task changing rate from {0} to {1}", delay, _blinkRate);
                                    delay = _blinkRate;
                                }
                            }
                            lock (_outputPins)
                            {
                                if (ActivePin != null)
                                {
                                    GPIOOutputPin p;
                                    if (_outputPins.TryGetValue(ActivePin, out p))
                                    {
                                        Log.WriteLine(" blink task toggling {0}", p.Index);
                                        p.Toggle();
                                    }
                                }
                            }
                            Thread.Sleep(delay);
                        } while (delay > 0);
                        Log.WriteLine("ending blink task");
                    });
                }
            }
        }
        public async Task UpdatePinConfigurationAsync(GpioPinIndexesType newPinIndexes)
        {
            await Task.Run(() => {
                Log.WriteLine("pin configuration updating");
                UpdateOutputPin(newPinIndexes.Blue, "blue");
                UpdateOutputPin(newPinIndexes.Green, "green");
                UpdateOutputPin(newPinIndexes.Red, "red");
                UpdateOutputPin(newPinIndexes.Yellow, "yellow");
                UpdateInputPin(newPinIndexes.Input, "input");
            });
        }
        GpioPinValue _activeValue;
        public GpioPinValue ActiveValue
        {
            get { return _activeValue; }
        }

        public void InvertOutputPins()
        {
            lock (_outputPins)
            {
                _defaultOutputState = _defaultOutputState.Invert();
                foreach (var p in _outputPins)
                {
                    p.Value.Toggle();
                }
            }
        }
        string _activePin;
        public string ActivePin {
            get { return _activePin;  }
            set {
                Log.WriteLine("setting activepin from {0} to {1}", _activePin != null ? _activePin : "(null)", value != null ? value : "(null)");
                lock (_outputPins)
                {
                    if (_activePin == value)
                    {
                        Log.WriteLine("set active pin ignoring request to set existing value {0}", (_activePin == null || !_outputPins.ContainsKey(_activePin)) ? "(null)" : _activePin);
                        return;
                    } else
                    {
                        Log.WriteLine("set active pin acting on new value");
                    }
                    if (_activePin != null && _outputPins.ContainsKey(_activePin))
                    {
                        var p = _outputPins[_activePin];
                        Log.WriteLine("toggling previous pin '{0}' {1}", _activePin, p.Value.Invert().ToStateString());
                        p.Toggle();
                    } else
                    {
                        Log.WriteLine("no previous active pin to clear");
                    }
                    _activePin = value;
                    if (_activePin != null && _outputPins.ContainsKey(_activePin))
                    {
                        var p = _outputPins[_activePin];
                        Log.WriteLine("turning new pin '{0}' {1}", _activePin, p.Value.Invert().ToStateString());
                        p.Toggle();
                    } else
                    {
                        Log.WriteLine("active pin missing from _outputPins. _activePin {0} null", _activePin == null ? "is" : "is not");
                    }
                }
            }
        }

        public void Test(TimeSpan testDuration, TimeSpan pinInterval)
        {
            Log.WriteLine("Test started");
            var t = DateTime.Now;
            while (DateTime.Now - t < testDuration)
            {
                Dictionary<string, GPIOOutputPin> local;
                lock (_outputPins)
                {
                    local = _outputPins;
                }
                foreach (var pin in local)
                {
                    GPIOOutputPin p = pin.Value;
                    p.Toggle();
                    Log.WriteLine("pin {0} {1}", p.Index, p.Value.ToString());
                    Thread.Sleep(pinInterval);
                }
            }
        }

    }
}
