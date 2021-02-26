using System;
using System.Diagnostics;
using System.Threading;
using System.Collections;
//using Bauland.Others;
using Driver.MfRc522;
using Driver.MfRc522.Constants;
using nanoFramework.Hardware.Esp32;



// using Bauland.Others.Constants.MfRc522;
//using GHIElectronics.TinyCLR.Devices.Gpio;
//using GHIElectronics.TinyCLR.Pins;

namespace testRC522
{
    static class Program
    {
        private static MfRc522 _mfRc522;
        static void Main()
        {
            Setup();
            
            Debug.WriteLine("###############################################");
            Debug.WriteLine("# SPI1");
            Debug.WriteLine("###############################################");
            Debug.WriteLine($"SPI1_CLOCK: " + Configuration.GetFunctionPin(DeviceFunction.SPI1_CLOCK));
            Debug.WriteLine($"SPI1_MISO: " + Configuration.GetFunctionPin(DeviceFunction.SPI1_MISO));
            Debug.WriteLine($"SPI1_MOSI: " + Configuration.GetFunctionPin(DeviceFunction.SPI1_MOSI));

            Debug.WriteLine("");  
            Debug.WriteLine($"Version: 0x{_mfRc522.GetVersion():X}");
            Debug.WriteLine("");

            //_mfRc522.Test();
            byte[] defaultKey = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            ArrayList writeData[];
            byte writeSector = 2;
            writeData[1] = "Test1";
            writeData[2] = " Test2";
            writeData[3] = "  Test3";

            ///InfiniteLoop(defaultKey);    

            WriteSector(writeData, writeSector, defaultKey); //for write test, comment this and uncomment InfiniteLoop()
        }
        
 
        private static void InfiniteLoop(byte[] key)
        {
            byte[] bufferAtqa = new byte[2];
            byte[] defaultKey = key;

            while (true)
            {
                //test
               // bufferAtqa[0] = 0x0;
                //bufferAtqa[1] = 0x0;
                if (_mfRc522.IsNewCardPresent(bufferAtqa))
                {
                   Debug.WriteLine("Card detected...");
                   Debug.WriteLine($"ATQA: 0x{bufferAtqa[1]:X2},0x{bufferAtqa[0]:X2}");

                    var uid = _mfRc522.PiccReadCardSerial();
                    if (uid != null)
                    {
                        DisplayUid(uid);
                        try
                        {
                            byte pageOrSector = (byte)(uid.GetPiccType() == PiccType.Mifare1K ? 16 : 4);
                            for (byte i = 0; i < pageOrSector; i++)
                            {
                               Debug.WriteLine($"{i}:");
                                var buffer = _mfRc522.GetSector(uid, i, defaultKey /*, PiccCommand.AuthenticateKeyA*/);
                                if (uid.GetPiccType() == PiccType.Mifare1K)
                                {
                                    var c = _mfRc522.GetAccessRights(buffer);
                                    Display1kBuffer(buffer, c);
                                }
                                else if (uid.GetPiccType() == PiccType.MifareUltralight)
                                {
                                    DisplayUltralightBuffer(buffer);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                           Debug.WriteLine(ex.Message);
                        }

                        _mfRc522.Halt();
                        _mfRc522.StopCrypto();
                    }
                }

                Thread.Sleep(1000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static byte[][] ReadSector(byte sector, byte[] key)
        {
            var uid = _mfRc522.PiccReadCardSerial();
            var buffer = _mfRc522.GetSector(uid, sector, key);
            return buffer;
        }

        private static void WriteSector(ArrayList data, byte sector, byte[] key)
        {

            var uid = _mfRc522.PiccReadCardSerial();
            _mfRc522.PutSector(data, uid, sector, key);
        }

        private static void DisplayUltralightBuffer(byte[][] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var line = "";
                for (int j = 0; j < buffer[i].Length; j++)
                {
                    line += $"{buffer[i][j]:X2} ";
                }
               Debug.WriteLine(line);
            }
        }

        private static void Display1kBuffer(byte[][] buffer, byte[] accessRights)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var line = "";
                for (int j = 0; j < buffer[i].Length; j++)
                {
                    line += $"{buffer[i][j]:X2} ";
                }
                line += $"[{(accessRights[0] >> i) & 0x01} {(accessRights[1] >> i) & 0x01} {(accessRights[2] >> i) & 0x01}]";
               Debug.WriteLine(line);
            }
        }

        private static void DisplayUid(Uid uid)
        {
            string msg = "Uid of card is: ";
            for (int i = 0; i < (int)uid.UidType; i++)
            {
                msg += $"{uid.UidBytes[i]:X2} ";
            }
            msg += $"SAK: {uid.Sak:X2}";
           Debug.WriteLine(msg);
            switch (uid.GetPiccType())
            {
                case PiccType.Mifare1K:
                   Debug.WriteLine("PICC type: MIFARE 1K");
                    break;
                case PiccType.MifareUltralight:
                   Debug.WriteLine("PICC type: MIFARE Ultralight");
                    break;
                default:
                   Debug.WriteLine("PICC type: Unknown");
                    break;
            }
        }
        
        private static void Setup()
        {
            _mfRc522 = new MfRc522("SPI1", 4, 5);
            // _mfRc522 = new MfRc522(FEZ.SpiBus.Spi1, FEZ.GpioPin.D8, FEZ.GpioPin.D9);
        }
    }
}