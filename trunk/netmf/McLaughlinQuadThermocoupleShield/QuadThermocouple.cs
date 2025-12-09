using System;
using Microsoft.SPOT;

namespace McLaughlinQuadThermocoupleShield
{
    public class QuadThermocouple
    {
        private System.Collections.ArrayList _thermocouples;
        private Microsoft.SPOT.Hardware.OutputPort _sclk;
        private Microsoft.SPOT.Hardware.InputPort _miso;
        private Microsoft.SPOT.Hardware.OutputPort _tc1_cs;
        private Microsoft.SPOT.Hardware.OutputPort _tc2_cs;
        private Microsoft.SPOT.Hardware.OutputPort _tc3_cs;
        private Microsoft.SPOT.Hardware.OutputPort _tc4_cs;

        private Object _chip_lock = new Object();

        public QuadThermocouple()
        {
            // initialize pins
            _sclk = new Microsoft.SPOT.Hardware.OutputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D13, false);
            _miso = new Microsoft.SPOT.Hardware.InputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D12, false, Microsoft.SPOT.Hardware.Port.ResistorMode.Disabled);
            _tc1_cs = new Microsoft.SPOT.Hardware.OutputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D10, true);
            _tc2_cs = new Microsoft.SPOT.Hardware.OutputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D9, true);
            _tc3_cs = new Microsoft.SPOT.Hardware.OutputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D8, true);
            _tc4_cs = new Microsoft.SPOT.Hardware.OutputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D7, true);

            _thermocouples = new System.Collections.ArrayList();
            _thermocouples.Add( new Thermocouple( 0, _chip_lock, _sclk, _miso, _tc1_cs));
            /*
            _thermocouples.Add( new Thermocouple( 1, _chip_lock, _sclk, _miso, _tc2_cs));
            _thermocouples.Add( new Thermocouple( 2, _chip_lock, _sclk, _miso, _tc3_cs));
            _thermocouples.Add( new Thermocouple( 3, _chip_lock, _sclk, _miso, _tc4_cs));
             */
        }

        public class ThermocoupleReading
        {
            public int Index { get; private set; }
            public double Temperature { get; private set; }
            public bool Open { get; private set; }
            public bool ShortedToGnd { get; private set; }
            public bool ShortedToVcc { get; private set; }

            public ThermocoupleReading( int index, double temp, bool open, bool shorted_to_gnd, bool shorted_to_vcc)
            {
                Index = index;
                Temperature = temp;
                Open = open;
                ShortedToGnd = shorted_to_gnd;
                ShortedToVcc = shorted_to_vcc;
            }
        }

        public System.Collections.ArrayList GetReadings()
        {
            System.Collections.ArrayList readings = new System.Collections.ArrayList();
            for( int i=0; i<_thermocouples.Count; i++) {
                try {
                    bool open;
                    bool shorted_to_gnd;
                    bool shorted_to_vcc;
                    Thermocouple tc = (Thermocouple)_thermocouples[i];
                    double temperature = tc.ReadCelcius( out open, out shorted_to_gnd, out shorted_to_vcc);
                    readings.Add( new ThermocoupleReading( tc.Index, temperature, open, shorted_to_gnd, shorted_to_vcc));
                } catch( Exception) {
                    // error, so use placeholder value.  I chose 2000 since that's what the sample Arduino code uses.
                    readings.Add( 2000);
                }
            }
            return readings;
        }

        public class Thermocouple
        {
            public int Index { get; private set; }
            private Microsoft.SPOT.Hardware.OutputPort _sclk;
            private Microsoft.SPOT.Hardware.InputPort _miso;
            private Microsoft.SPOT.Hardware.OutputPort _cs;
            private Object _spi_lock;

            public Thermocouple( int index, Object spi_lock, Microsoft.SPOT.Hardware.OutputPort sclk,
                                 Microsoft.SPOT.Hardware.InputPort miso, Microsoft.SPOT.Hardware.OutputPort cs)
            {
                Index = index;
                _sclk = sclk;
                _miso = miso;
                _cs = cs;
                _spi_lock = spi_lock;
            }

            public double ReadCelcius( out bool open, out bool shorted_to_gnd, out bool shorted_to_vcc)
            {
                // write command here to get value
                int data = ReadData();
                // the TC info is the most significant word, and the reference junction is the least significant
                // let's make life easier and throw out the reference junction info
                short tc = (short)((data & 0xFFFF0000) >> 16);
                // bit 31 is the sign bit
                bool negative = (tc & 0x8000) != 0;
                // bit 16 is the shorted bit
                open = (tc & 0x0001) != 0;
                shorted_to_gnd = (tc & 0x0002) != 0;
                shorted_to_vcc = (tc & 0x0004) != 0;
                // get the stuff on the left side of the decimal, bits 30:20
                int left = (tc & 0x7FF0) >> 4;
                // get the stuff on the right side of the decimal
                double right = ((tc & 0x0008) >> 3) / 2.0 + ((tc & 0x0004) >> 2) / 4.0;
                return negative ? -((double)left + right) : (double)left + right;
            }

            private int ReadData()
            {
                // see datasheet for MAX31855: http://datasheets.maxim-ic.com/en/ds/MAX31855.pdf
                // the chip sends out 32 bits of data
                int data = 0;
                lock( _spi_lock) {
                    // data is returned MSb first, so loop backwards
                    _cs.Write( false);
                    for( int i=31; i>=0; i--) {
                        _sclk.Write( false);
                        if( _miso.Read())
                            data |= (1 << i);
                        _sclk.Write( true);
                    }
                    _cs.Write( true);
                }
                return data;
            }
        }
    }
}
