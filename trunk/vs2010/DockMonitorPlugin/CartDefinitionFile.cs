using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.Plugins.Dock
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    public class CartDefinitionFile
    {
        public CartSerialization serialization { get; set; }

        private static readonly ILog _log = LogManager.GetLogger(typeof(DockMonitorPlugin));
        private const string folder = @"\config\";
        private const string file = "cart_definitions";
        private const string ext = ".xml";

        public CartDefinitionFile()
        {
            /*
            // this was just used to write out XML so I could figure out what it looks like
            XmlSerializer serializer = new XmlSerializer( typeof( CartSerialization));
            FileStream writer = new FileStream( "sample_cart_definition_file.xml", FileMode.Create);
            CartSerialization temp = new CartSerialization();
            temp.RacksAndSlots.Add( new RackAndSlotInfo { cart_id="joe", NumberOfRacks=5, NumberOfSlots=12 });
            temp.RacksAndSlots.Add( new RackAndSlotInfo { cart_id="bob", NumberOfRacks=4, NumberOfSlots=7 });
            serializer.Serialize( writer, temp);
             */

            try
            {
                var path = (folder + file + ext).ToAbsoluteAppPath();
                XmlSerializer serializer = new XmlSerializer(typeof(CartSerialization));
                FileStream reader = new FileStream(path, FileMode.Open);
                serialization = (CartSerialization)serializer.Deserialize(reader);
                reader.Close();
            }
            catch (FileNotFoundException ex)
            {
                _log.ErrorFormat( "Could not load cart definition file: {0}", ex.Message);
            }
        }

        /// <summary>
        /// returns the number of racks and slots in a given cart
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="num_racks"></param>
        /// <param name="num_slots"></param>
        /// <exception cref="NullReferenceException">
        /// </exception>
        public void GetNumberOfRacksAndSlotsFromBarcode(string barcode, out int num_racks, out int num_slots)
        {
            var info = serialization.RacksAndSlots.FirstOrDefault(x => x.cart_barcode == barcode);
            if (info == null)
                throw new CartIdNotFoundException(barcode);
            num_racks = info.NumberOfRacks;
            num_slots = info.NumberOfSlots;
        }

        public void GetCartIdentifiersFromBarcode(string barcode, out string human_readable, out string file_prefix)
        {
            var info = serialization.RacksAndSlots.FirstOrDefault(x => x.cart_barcode == barcode);
            if (info == null)
                throw new CartIdNotFoundException(barcode);
            human_readable = info.cart_human_readable == "" ? barcode : info.cart_human_readable;
            file_prefix = info.cart_file_prefix == "" ? barcode : info.cart_file_prefix;
        }

        #region CartDefinitionEditor utility methods
        public IEnumerable<string> GetCartBarcodes()
        {
            if (serialization == null)
                return new List<string>();
            return serialization.RacksAndSlots.Select(x => x.cart_barcode);
        }

        public void MakeBackup()
        {
            var source = (folder + file + ext).ToAbsoluteAppPath();
            var datestamp = DateTime.Now.ToString("_yyyy-MM-dd-HH_mm_ss");
            var dest = (folder + file + datestamp + ext).ToAbsoluteAppPath();
            File.Copy(source, dest);
        }

        private void WriteFile()
        {
            var path = (folder + file + ext).ToAbsoluteAppPath();
            var serializer = new XmlSerializer(typeof(CartSerialization));
            var writer = new FileStream(path, FileMode.Create);
            serializer.Serialize(writer, serialization);
        }

        public bool AddCartDefinition(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;
            if (GetCartBarcodes().Contains(barcode))
                return false;

            string default_prefix = "cart" + barcode;
            string default_human = barcode;
            int default_racks = 5;
            int default_slots = 12;

            serialization.RacksAndSlots.Add(new RackAndSlotInfo()
            {
                cart_barcode = barcode,
                cart_file_prefix = default_prefix,
                cart_human_readable = default_human,
                NumberOfRacks = default_racks,
                NumberOfSlots = default_slots
            });

            WriteFile();

            return true;
        }

        public void RemoveCartDefinition(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return;
            if (!GetCartBarcodes().Contains(barcode))
                return;

            serialization.RacksAndSlots.RemoveAll((x) => x.cart_barcode == barcode);
            WriteFile();
        }

        public void UpdateCartDefinition(string barcode, string human_readable, string file_prefix, int racks, int slots)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return;
            if (!GetCartBarcodes().Contains(barcode))
                return;

            var info = serialization.RacksAndSlots.FirstOrDefault(x => x.cart_barcode == barcode);
            if (info == null)
                return;

            info.cart_human_readable = human_readable;
            info.cart_file_prefix = file_prefix;
            info.NumberOfRacks = racks;
            info.NumberOfSlots = slots;

            WriteFile();
        }
        #endregion
    }

    public class CartSerialization
    {
        public List<RackAndSlotInfo> RacksAndSlots { get; set; }

        public CartSerialization()
        {
            RacksAndSlots = new List<RackAndSlotInfo>();
        }
    }

    public class RackAndSlotInfo
    {
        //! \todo need to figure out if the XmlSerializer can handle dictionaries (I think not)
        public string cart_barcode { get; set; }         // what we actually scan on the cart, and what we look up in the cart definition file to get racks / slots
        public string cart_human_readable { get; set; }  // identifier for UI, only used on the UI
        public string cart_file_prefix { get; set; }     // what we use to name the cart related files, returned by GetTeachpointFilePrefix
        public int NumberOfRacks { get; set; }
        public int NumberOfSlots { get; set; }
    }

    public class CartIdNotFoundException : Exception
    {
        public string Id { get; set; }

        public CartIdNotFoundException(string id)
            : base(String.Format("The cart definition file cart_definitions.xml does not contain information for a cart '{0}'", id))
        {
            Id = id;
        }
    }
}
