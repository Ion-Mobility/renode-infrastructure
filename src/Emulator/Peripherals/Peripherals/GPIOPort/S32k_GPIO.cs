//
// Copyright (c) 2020 LabMICRO FACET UNT
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
// using System;
// using System.Linq;
// using System.Collections.Generic;
// using Antmicro.Renode.Core;
// using Antmicro.Renode.Core.Structure.Registers;
// using Antmicro.Renode.Peripherals.Bus;
// using Antmicro.Renode.Utilities;

// namespace Antmicro.Renode.Peripherals.GPIOPort
// {
//     public class S32K1x_GPIO : BaseGPIOPort, IProvidesRegisterCollection<DoubleWordRegisterCollection>, IDoubleWordPeripheral, IKnownSize
//     {
//         public S32K1x_GPIO(Machine machine) : base(machine, PinsPerPort * NumberOfPorts)
//         {
//             RegistersCollection = new DoubleWordRegisterCollection(this);

//             ports = new Port[NumberOfPorts];
//             for(var portNumber = 0; portNumber < ports.Length; portNumber++)
//             {
//                 ports[portNumber] = new Port(portNumber, this);
//             }

//             Reset();
//         }

//         public uint ReadDoubleWord(long offset)
//         {
//             return RegistersCollection.Read(offset);
//         }

//         public void WriteDoubleWord(long offset, uint value)
//         {
//             RegistersCollection.Write(offset, value);
//         }

//         public override void Reset()
//         {
//             base.Reset();
//             RegistersCollection.Reset();
//         }

//         public long Size => 0x2380;

//         public DoubleWordRegisterCollection RegistersCollection { get; }

//         private readonly Port[] ports;

//         private const int NumberOfPorts = 5;
//         private const int PinsPerPort = 32;

//         private class Port
//         {
//             public Port(int portNumber, S32K1x_GPIO parent)
//             {
//                 this.portNumber = portNumber;
//                 this.parent = parent;

//                 parent.RegistersCollection.DefineRegister((uint)Registers.Direction + 4 * portNumber)
//                     .WithValueField(0, PinsPerPort, out direction, name: $"GPIO_DIR{portNumber}",
//                         writeCallback: (_, value) => RefreshConnectionsState());

//                 parent.RegistersCollection.DefineRegister((uint)Registers.Mask + 4 * portNumber)
//                     .WithValueField(0, PinsPerPort, out mask, name: $"GPIO_MASK{portNumber}");

//                 parent.RegistersCollection.DefineRegister((uint)Registers.Pin + 4 * portNumber)
//                     .WithValueField(0, PinsPerPort, out state, name: $"GPIO_PIN{portNumber}",
//                         writeCallback: (_, value) => RefreshConnectionsState(),
//                         valueProviderCallback: _ => GetStateValue());

//                 parent.RegistersCollection.DefineRegister((uint)Registers.MaskedPin + 4 * portNumber)
//                     .WithValueField(0, PinsPerPort, name: $"GPIO_MPIN{portNumber}",
//                         writeCallback: (_, value) => SetStateValue(state.Value & mask.Value | value & ~mask.Value),
//                         valueProviderCallback: _ => GetStateValue() & ~mask.Value);

//                 parent.RegistersCollection.DefineRegister((uint)Registers.SetPin + 4 * portNumber)
//                     .WithValueField(0, PinsPerPort, name: $"GPIO_SET{portNumber}",
//                         writeCallback: (_, value) => SetStateValue(state.Value | value),
//                         valueProviderCallback: _ => GetStateValue());

//                 parent.RegistersCollection.DefineRegister((uint)Registers.ClearPin + 4 * portNumber)
//                     .WithValueField(0, PinsPerPort, FieldMode.Write, name: $"GPIO_CLR{portNumber}",
//                         writeCallback: (_, value) => SetStateValue(state.Value & ~value));

//                 parent.RegistersCollection.DefineRegister((uint)Registers.NegatePin + 4 * portNumber)
//                     .WithValueField(0, PinsPerPort, FieldMode.Write, name: $"GPIO_NOT{portNumber}",
//                         writeCallback: (_, value) => SetStateValue(state.Value ^ value));
//             }

//             private UInt32 GetStateValue()
//             {
//                 UInt32 result = 0;

//                 for(byte bitIndex = 0; bitIndex < PinsPerPort; bitIndex++)
//                 {
//                     var idx = PinsPerPort * portNumber + bitIndex;
//                     var isOutputPin = BitHelper.IsBitSet(direction.Value, bitIndex);

//                     BitHelper.SetBit(ref result, bitIndex, isOutputPin
//                         ? parent.Connections[idx].IsSet
//                         : parent.State[idx]);
//                 }

//                 return result;
//             }

//             private void SetStateValue(UInt32 value)
//             {
//                 state.Value = value;
//                 RefreshConnectionsState();
//             }

//             private void RefreshConnectionsState()
//             {
//                 for(byte bitIndex = 0; bitIndex < PinsPerPort; bitIndex++)
//                 {
//                     if(BitHelper.IsBitSet(direction.Value, bitIndex))
//                     {
//                         var connection = parent.Connections[PinsPerPort * portNumber + bitIndex];
//                         var pinState = BitHelper.IsBitSet(state.Value, bitIndex);

//                         connection.Set(pinState);
//                     }
//                 }
//             }

//             private readonly int portNumber;
//             private readonly IValueRegisterField direction;
//             private readonly IValueRegisterField mask;
//             private readonly IValueRegisterField state;

//             private readonly S32K1x_GPIO parent;
//         }

//         private enum Registers
//         {
//             Direction = 0x2000,
//             Mask = 0x2080,
//             Pin = 0x2100,
//             MaskedPin = 0x2180,
//             SetPin = 0x2200,
//             ClearPin = 0x2280,
//             NegatePin = 0x2300,
//         }
//     }
// }





//
// Copyright (c) 2010-2021 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System.Collections.Generic;
using Antmicro.Renode.Core;
using Antmicro.Renode.Utilities;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;

namespace Antmicro.Renode.Peripherals.GPIOPort
{
    public class S32K1x_GPIO : BaseGPIOPort, IDoubleWordPeripheral, IKnownSize
    {
        public S32K1x_GPIO(Machine machine) : base(machine, NumberOfPins)
        {
            locker = new object();
            IRQ = new GPIO();
            registers = new DoubleWordRegisterCollection(this, BuildRegisterMap());
            data = new bool[NumberOfPins];
            directionOutNotIn = new bool[NumberOfPins];
            interruptEnabled = new bool[NumberOfPins];
            interruptRequest = new bool[NumberOfPins];
            edgeSelect = new bool[NumberOfPins];
            interruptConfig = new InterruptConfig[NumberOfPins];
        }

        public override void Reset()
        {
            lock(locker)
            {
                base.Reset();
                IRQ.Unset();
                registers.Reset();
                for(var i = 0; i < NumberOfPins; ++i)
                {
                    data[i] = false;
                    directionOutNotIn[i] = false;
                    interruptEnabled[i] = false;
                    interruptRequest[i] = false;
                    edgeSelect[i] = false;
                    interruptConfig[i] = InterruptConfig.Low;
                }
            }
        }

        public uint ReadDoubleWord(long offset)
        {
            lock(locker)
            {
                return registers.Read(offset);
            }
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            lock(locker)
            {
                registers.Write(offset, value);
            }
        }

        public override void OnGPIO(int number, bool value)
        {
            if(!CheckPinNumber(number))
            {
                return;
            }

            if(directionOutNotIn[number])
            {
                this.Log(LogLevel.Warning, "gpio {0} is set to output, signal ignored.", number);
                return;
            }

            lock(locker)
            {
                var previousState = State[number];
                base.OnGPIO(number, value);

                UpdateSingleInterruptRequest(number, value, previousState != value);
                UpdateIRQ();
            }
        }

        public long Size => 0x90;

        public GPIO IRQ { get; }

        private Dictionary<long, DoubleWordRegister> BuildRegisterMap()
        {
            var registersDictionary = new Dictionary<long, DoubleWordRegister>
            {
                {(long)Registers.PDOR, new DoubleWordRegister(this)
                    .WithFlags(0, NumberOfPins, name: "PDOR / Port Data Output Register",
                        writeCallback: (id, _, val) => { data[id] = val; },
                        valueProviderCallback: (id, _) =>
                        {
                            return (directionOutNotIn[id])
                                ? data[id]
                                : Connections[id].IsSet;
                        })
                    .WithWriteCallback((_, __) => UpdateConnections())
                },
                {(long)Registers.PSOR, new DoubleWordRegister(this)
                    .WithFlags(0, NumberOfPins, FieldMode.Write, name: "PSOR / Port Set Output Register",
                        writeCallback: (id, _, __)  => { data[id] = true; })
                    .WithWriteCallback((_, __) => UpdateConnections())
                },
                {(long)Registers.PCOR, new DoubleWordRegister(this)
                    .WithFlags(0, NumberOfPins, FieldMode.Write, name: "PCOR / Port Clear Output Register",
                        writeCallback: (id, _, __)  => { data[id] = false; })
                    .WithWriteCallback((_, __) => UpdateConnections())
                },
                {(long)Registers.PTOR, new DoubleWordRegister(this)
                    .WithFlags(0, NumberOfPins, FieldMode.Write, name: "PTOR / Port Toggle Output Register",
                        writeCallback: (id, _, __)  => { data[id] ^= true; })
                    .WithWriteCallback((_, __) => UpdateConnections())
                },

                {(long)Registers.PDDR, new DoubleWordRegister(this)
                    .WithFlags(0, NumberOfPins, name: "PDDR / Port Data Direction Register",
                        writeCallback: (id, _, val) => { directionOutNotIn[id] = val; },
                        valueProviderCallback: (id, _) => directionOutNotIn[id])
                    .WithWriteCallback((_, __) => UpdateConnections())
                },
                {(long)Registers.PadStatus, new DoubleWordRegister(this)
                    .WithFlags(0, NumberOfPins, FieldMode.Read, name: "PSR / GPIO pad status register",
                        valueProviderCallback: (id, _) => Connections[id].IsSet)
                },
                // {(long)Registers.Mask, new DoubleWordRegister(this)
                //     .WithFlags(0, NumberOfPins, name: "IMR / GPIO interrupt mask register",
                //         writeCallback: (id, _, val) => { interruptEnabled[id] = val; },
                //         valueProviderCallback: (id, _) => interruptEnabled[id])
                //     .WithWriteCallback((_, __) => UpdateIRQ())
                // },
                {(long)Registers.Status, new DoubleWordRegister(this)
                    .WithFlags(0, NumberOfPins, FieldMode.Read | FieldMode.WriteOneToClear, name: "ISR / GPIO interrupt status register",
                        writeCallback: (id, _, val) =>
                        {
                            if(val)
                            {
                                interruptRequest[id] = false;
                            }
                        },
                        valueProviderCallback: (id, _) => interruptRequest[id])
                    .WithWriteCallback((_, __) => UpdateIRQ())
                },
                // {(long)Registers.EdgeSelect, new DoubleWordRegister(this)
                //     .WithFlags(0, NumberOfPins, name: "EDGE_SEL / GPIO edge select register",
                //         writeCallback: (id, _, val) => { edgeSelect[id] = val; },
                //         valueProviderCallback: (id, _) => edgeSelect[id])
                // },

            };

            var config1 = new DoubleWordRegister(this);
            var config2 = new DoubleWordRegister(this);
            var half = NumberOfPins / 2;
            for(var i = 0; i < half; ++i)
            {
                var j = i;
                config1.WithEnumField<DoubleWordRegister, InterruptConfig>(j * 2, 2,
                    name: $"ICR{j} / Interrupt configuration {j}",
                    writeCallback: (_, val) => { interruptConfig[j] = val; },
                    valueProviderCallback: _ => interruptConfig[j]);
                config2.WithEnumField<DoubleWordRegister, InterruptConfig>(j * 2, 2,
                    name: $"ICR{half + j} / Interrupt configuration {half + j}",
                    writeCallback: (_, val) => { interruptConfig[half + j] = val; },
                    valueProviderCallback: _ => interruptConfig[half + j]);
            }
            config1.WithWriteCallback((_, __) => UpdateAllInterruptRequests());
            config2.WithWriteCallback((_, __) => UpdateAllInterruptRequests());
            registersDictionary.Add((long)Registers.Config1, config1);
            registersDictionary.Add((long)Registers.Config2, config2);
            return registersDictionary;
        }

        private void UpdateIRQ()
        {
            var flag = false;
            for(var i = 0; i < NumberOfPins; ++i)
            {   
                flag |= interruptEnabled[i] && interruptRequest[i];
            }
            IRQ.Set(flag);
        }

        private void UpdateConnections()
        {
            for(var i = 0; i < NumberOfPins; ++i)
            {
                Connections[i].Set(directionOutNotIn[i] && data[i]);
            }
            UpdateIRQ();
        }

        private void UpdateAllInterruptRequests()
        {
            for(var i = 0; i < NumberOfPins; ++i)
            {
                UpdateSingleInterruptRequest(i, State[i]);
            }
            UpdateIRQ();
        }

        private void UpdateSingleInterruptRequest(int i, bool currentState, bool stateChanged = false)
        {
            if(edgeSelect[i])
            {
                interruptRequest[i] |= stateChanged;
            }
            else
            {
                switch(interruptConfig[i])
                {
                    case InterruptConfig.Low:
                        interruptRequest[i] |= !currentState;
                        break;
                    case InterruptConfig.High:
                        interruptRequest[i] |= currentState;
                        break;
                    case InterruptConfig.Rising:
                        interruptRequest[i] |= stateChanged && currentState; 
                        break;
                    case InterruptConfig.Falling:
                        interruptRequest[i] |= stateChanged && !currentState; 
                        break;
                    default:
                        this.Log(LogLevel.Error, "Invalid state (interruptConfig[{0}]: 0x{1:X}).", i, interruptConfig[i]);
                        break;
                }
            }
        }

        private readonly DoubleWordRegisterCollection registers;
        private readonly object locker;
        private readonly bool[] data;
        private readonly bool[] directionOutNotIn;
        private readonly bool[] interruptEnabled;
        private readonly bool[] interruptRequest;
        private readonly bool[] edgeSelect;
        private readonly InterruptConfig[] interruptConfig;

        private const int NumberOfPins = 32;

        private enum InterruptConfig
        {
            Low = 0b00,
            High = 0b01,
            Rising = 0b10,
            Falling = 0b11,
        }

        private enum Registers : long
        {
            // Data = 0x0,
            // Direction = 0x4,
            // PadStatus = 0x8,
            // Config1 = 0xc,
            // Config2 = 0x10,
            // Mask = 0x14,
            // Status = 0x18,
            // EdgeSelect = 0x1c,
            // DataSet = 0x84,
            // DataClear = 0x88,
            // DataToggle = 0x8c,

            PDOR = 0x0,         //Port Data Output Register
            PSOR = 0x4,         //Port Set Output Register
            PCOR = 0x8,         //Port Clear Output Register
            PTOR = 0xc,         //Port Toggle Output Register
            PDIR = 0x10,         //Port Data Input Register
            PDDR = 0x14,         //Port Data Direction Register
            PIDR = 0x18,         //Port Input Disable Register

            // Config1 = 0xc,
            // Config2 = 0x10,
            // Mask = 0x14,
            // Status = 0x18,
            // EdgeSelect = 0x1c,
            // DataSet = 0x84,
            // DataClear = 0x88,
            // DataToggle = 0x8c,

        }
    }
}
