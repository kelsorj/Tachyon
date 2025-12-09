using System;
using System.IO.Ports;
using System.Threading;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public static class SerialPortExtensions
    {
        /// <summary>
        /// Sends a command to a device connected to the serial port, and then does the handshaking to
        /// receive the requested data and save it into the specified filename
        /// </summary>
        /// <remarks>
        /// see http://msdn.microsoft.com/en-us/library/ms817599.aspx
        /// </remarks>
        /// <param name="port"></param>
        /// <param name="command_to_sending_device">command to send to the RS-232 device</param>
        /// <param name="expect_header_from_device">whether or not the connected device will be sending a header with the filename to be transferred</param>
        public static byte[] ReceiveFile( this SerialPort port, string command_to_sending_device, bool expect_header_from_device)
        {
            try {
                // make sure we tell the reader to cancel the transfer or it will look like it's hanging
                // during normal operation.
                port.Write( "\x18");

                int last_packet_number = -1; // initial value, this gets incremented with each packet
                const int wait_for_data_delay_ms = 200;

                // send the command to the device
                port.DiscardInBuffer();
                port.Write( command_to_sending_device);
                Thread.Sleep( wait_for_data_delay_ms);
                // if we're expecting the filename header to come back, process it
                if( expect_header_from_device) {
                    // the header looks like this:
                    //      1 byte           1 byte             1 byte            128 or 1024 bytes   1 byte    1 byte
                    // <start of header><packet number><packet number complement><     filename    ><CRC, MSB><CRC, LSB>
                
                    // read in one byte first, so we know how big the packet is
                    port.Write( "\x43");
                    Thread.Sleep( wait_for_data_delay_ms);
                    byte[] filename = ReadYModemPacket( port, ref last_packet_number);
                }

                const int max_filesize = 3145728;
                byte[] image_buffer = new byte[max_filesize];
                // since we allocate a ton of memory up front, we can only append data by
                // keeping track of how much image data was already copied over
                int total_image_bytes_copied = 0;

                // send the 0x0643 first to get the data transfer running.  afterward, we have to use C.  I am
                // going with a Microscan-specific implementation right now since it doesn't follow the Hyperterminal
                // protocol exactly.
                port.DiscardInBuffer();
                port.Write( "\x06\x43");
                Thread.Sleep( wait_for_data_delay_ms);

                while( true) {
                    byte[] image_data_fragment = ReadYModemPacket( port, ref last_packet_number);
                    // if we don't get any data from ReadYModemPacket, that means that we got an
                    // EOT from the device, so we are done with the transfer
                    if( image_data_fragment.Length == 0)
                        break;
                    // append buffer to file buffer
                    total_image_bytes_copied += image_data_fragment.Length;
                    Array.Copy( image_data_fragment, 0, image_buffer, total_image_bytes_copied, image_data_fragment.Length);
                    port.DiscardInBuffer();
                    port.Write( "\x43");
                    Thread.Sleep( wait_for_data_delay_ms);
                }

                // we send back an ACK (0x06)
                port.Write( "\x06");

                // write the file
                byte[] image = new byte[total_image_bytes_copied];
                Array.ConstrainedCopy( image_buffer, 0, image, 0, total_image_bytes_copied);
                return image;
            } catch( Exception) {
                // make sure we tell the reader to cancel the transfer or it will look like it's hanging
                // during normal operation.
                port.Write( "\x18");
                throw;
            }

        }

        public static byte[] ReadYModemPacket( SerialPort port, ref int last_packet_number)
        {
            byte soh = (byte)port.ReadByte();
            // soh determines the size of the payload, but first we have to make sure we don't 
            // get an EOT, which means the device is done sending all data
            if( soh == 0x04) {
                return new byte[0];
            }

            int payload_size = (soh == 1 ? 128 : 1024);
            byte[] data = new byte[payload_size + 5];
            // to keep the buffer consistent with the hyperterm ymodem spec, put soh into the buffer
            data[0] = soh;
            // add 4 to data_size instead of 5 because we already read in the soh byte
            int bytes_read = port.Read( data, 1, payload_size + 4);
            // confirm data received
            if( bytes_read != payload_size + 4)
                throw new Exception( "Did not receive all of the header bytes");
            // confirm packet number
            // remember this is a little weird compared to the hyperterm docs, because SOH is really the first byte,
            // but since I read it out early, now packet number is the first byte.
            if( data[1] != ++last_packet_number)
                throw new Exception( "Packet number is out of order");
            if( data[2] != (byte)(255 - last_packet_number))
                throw new Exception( "Packet number complement is incorrect");
            // for now, don't care about the filename
            byte[] payload_data = new byte[payload_size];
            Array.Copy( data, 3, payload_data, 0, payload_size);
            // check CRC later
            byte crc_high = data[payload_size + 3];
            byte crc_low = data[payload_size + 4];

            return payload_data;
        }
    }
#endif
}
