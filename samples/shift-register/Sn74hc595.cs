using System;
using System.Device.Gpio;

namespace Iot.Device.ShiftRegister
{
    public class Sn74hc595
    {

        // https://www.ti.com/lit/ds/symlink/sn74hc595.pdf

        private GpioController _controller;
        private bool _shouldDispose;
        private PinMapping _mapping;
        private int _SER;
        private int _SRCLK;
        private int _RCLK;

        private int _length;

        public Sn74hc595(PinMapping pinMapping, GpioController controller = null,  bool shouldDispose = true, int additionalRegisters = 0)
        {
            if (controller == null)
            {
                controller = new GpioController();
            }
            _controller = controller;
            _shouldDispose = shouldDispose;
            if (!pinMapping.Validate())
            {
                throw new ArgumentException(nameof(Sn74hc595));
            }
            _mapping = pinMapping;
            _SER = _mapping.SER;
            _SRCLK = _mapping.SRCLK;
            _RCLK = _mapping.RCLK;
            _length = additionalRegisters + 1;
            Setup();
        }

        public int Length => _length;

        public void Dispose()
        {
            if (_shouldDispose)
            {
                _controller?.Dispose();
                _controller = null;
            }
        }

        public void ClearStorage()
        {
            if (_mapping.SRCLR > 0)
            {
                _controller.WriteValuesToPin(_mapping.SRCLR,0,1);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _length; i++)
            {
                ShiftAndLatch(0);
            }
        }

        public void OutputDisable()
        {
            if (_mapping.OE > 0)
            {
                _controller.Write(_mapping.OE, 1);
            }
        }

        public void OutputEnable()
        {
            if (_mapping.OE > 0)
            {
                _controller.Write(_mapping.OE, 0);
            }
        }


        public void Shift(PinValue value)
        {
            _controller.Write(_SER,value);
            _controller.Write(_SRCLK, 1);
            _controller.WriteValueToPins(0, _SER, _SRCLK);
        }

        public void Latch()
        {
            _controller.WriteValuesToPin(_RCLK,1,0);
        }

        public void ShiftAndLatch(byte data)
        {
            Console.WriteLine(nameof(ShiftAndLatch));
            for (int i = 0; i < 8; i++)
            {
                var value =  128 >> i & data;
                Console.WriteLine($"Index: {i}; Value: {value}");
                Shift((int)value);
            }
            Latch();
        }

        private void Setup()
        {
            _controller.OpenPins(PinMode.Output, _SER,_RCLK, _SRCLK, _mapping.SRCLR);
            _controller.WriteValueToPins(0, _SER, _RCLK, _RCLK);
            _controller.Write(_mapping.SRCLR,1);

            if (_mapping.OE > 0)
            {
                _controller.OpenPinAndWrite(_mapping.OE, 0);
            }

            if (_mapping.QOUT > 0)
            {
                _controller.OpenPinAndWrite(_mapping.QOUT, 0);
            }
        }

        public struct PinMapping
        {
            public PinMapping(int ser, int oe, int rclk, int srclk, int srclr, int qout)
            {
                SER = ser;
                RCLK = rclk;
                SRCLK = srclk;
                SRCLR = srclr;
                OE = oe;
                QOUT = qout;    
            }

            public static PinMapping Standard => new PinMapping(14,13,12,11,10,0);

            /*
                Mapping
                var SER = 14;   // data in
                var OE = 13;    // blank  
                var RCLK = 12;  // latch
                var SRCLK = 11; // clock
                var SRCLR =10;  // clear
                var QHOUT =9;   // data out
            */

            public int SER {get; set;}
            public int OE {get; set;}
            public int RCLK {get; set;}
            public int SRCLK {get; set;}
            public int SRCLR {get; set;}
            public int QOUT {get; set;}

            public bool Validate()
            {
                if (SER > 0 && 
                    RCLK > 0 &&
                    SRCLK > 0
                    )
                {
                    return true;
                }
                return false;
            }
        }
    }
}