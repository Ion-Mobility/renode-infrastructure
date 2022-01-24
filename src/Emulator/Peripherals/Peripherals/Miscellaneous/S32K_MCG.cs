//
// Copyright (c) 2010-2020 Antmicro
//
//  This file is licensed under the MIT License.
//  Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Collections.Generic;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    // MCG = Multipurpose Clock Generator
    public class S32K_MCG : IDoubleWordPeripheral, IKnownSize
    {
        public S32K_MCG()
        {
            var registersMap = new Dictionary<long, DoubleWordRegister>
            {
                {
                    (long)Registers.SCG_CSR, new DoubleWordRegister(this)
                        .WithValueField(24, 4,  valueProviderCallback: (_) => 3, name: "SCS")
                        .WithValueField(16, 4,  valueProviderCallback: (_) => 0, name: "DIVCORE")
                        .WithValueField(4, 4,  valueProviderCallback: (_) => 1, name: "DIVBUS")
                        .WithValueField(0, 4,  valueProviderCallback: (_) => 3, name: "DIVSLOW")
                },
                {
                    (long)Registers.SCG_SOSCCSR, new DoubleWordRegister(this)
                        .WithFlag(26, valueProviderCallback: (_) => false, name: "SOSCERR")
                        .WithFlag(25, valueProviderCallback: (_) => true, name: "SOSCSEL")
                        .WithFlag(24, valueProviderCallback: (_) => true, name: "SOSCVLD")
                        .WithFlag(23, valueProviderCallback: (_) => false, name: "LK")
                        .WithFlag(17, valueProviderCallback: (_) => false, name: "SOSCCMRE")
                        .WithFlag(16, valueProviderCallback: (_) => false, name: "SOSCCM")
                        .WithFlag(0, valueProviderCallback: (_) => true, name: "SOSCEN")
                },
                {
                    (long)Registers.SCG_FIRCCSR, new DoubleWordRegister(this)
                        .WithFlag(26, valueProviderCallback: (_) => false, name: "FIRCERR")
                        .WithFlag(25, valueProviderCallback: (_) => true, name: "FIRCSEL")
                        .WithFlag(24, valueProviderCallback: (_) => true, name: "FIRCVLD")
                        .WithFlag(23, valueProviderCallback: (_) => false, name: "LK")
                        .WithFlag(3, valueProviderCallback: (_) => false, name: "FIRCREGOFF")
                        .WithFlag(0, valueProviderCallback: (_) => true, name: "FIRCEN")
                },
            };

            registers = new DoubleWordRegisterCollection(this, registersMap);
        }

        public void Reset()
        {
            registers.Reset();
        }

        public uint ReadDoubleWord(long offset)
        {
            return registers.Read(offset);
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            registers.Write(offset, value);
        }

        public long Size => 0x1001;

        private readonly DoubleWordRegisterCollection registers;
        private readonly IEnumRegisterField<ClockSourceValues> clockSource;
        private readonly IEnumRegisterField<MCGPLLClockStatusValues> mcgPllStatus;
        private readonly IEnumRegisterField<PLLSelectValues> pllSelected;

        private enum Registers
        {
            SCG_VERID = 0x0,
            SCG_PARAM = 0x4,
            SCG_CSR = 0x10,
            SCG_SOSCCSR = 0x100,
            SCG_FIRCCSR = 0x300,
            SCG_FIRCDIV = 0x304,
            SCG_FIRCCFG = 0x308,
        }

        private enum ClockSourceValues
        {
            Either = 0,
            Internal = 1,
            External = 2,
            Reserved = 3
        }

        private enum MCGPLLClockStatusValues
        {
            Inactive = 0,
            Active = 1
        }

        private enum PLLSelectValues
        {
            FLLSelected = 0,
            PLLSelected = 1
        }

        private enum ClockModeStatusValues
        {
            FLL = 0,
            InternalClock = 1,
            ExternalClock = 2,
            PLL = 3
        }
    }
}