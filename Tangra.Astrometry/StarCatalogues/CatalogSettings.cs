﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;
using Astro.Utilities.StarCatalogues.NOMAD;
using Astro.Utilities.StarCatalogues.UCAC2;
using Astro.Utilities.StarCatalogues.UCAC3;

namespace Astro.Utilities.StarCatalogues
{
    public enum StarCatalog
    {
        NotSpecified = 0,
        UCAC2 = 1,
        NOMAD = 2,
        UCAC3 = 3
    }

    public class CatalogSettings
    {
        public CatalogSettings()
        { }

        public StarCatalog Catalog = StarCatalog.NotSpecified;
        public string CatalogLocation = string.Empty;

        public CatalogSettings(BinaryReader reader, byte configVersion)
        {
            byte savedVersion = reader.ReadByte();

            if (savedVersion > 0)
            {
                Catalog = (StarCatalog) reader.ReadInt32();
                CatalogLocation = reader.ReadString();
            }
        }

        private readonly byte CURRENT_VERSION = 1;

        public void Save(BinaryWriter writer)
        {
            writer.Write(CURRENT_VERSION);

            writer.Write((int) Catalog);
            writer.Write(CatalogLocation ?? string.Empty);
        }

        public static bool IsValidCatalogLocation(StarCatalog catalog, ref string folderPath)
        {
            if (catalog == StarCatalog.UCAC2)
                return UCAC2Catalogue.IsValidCatalogLocation(ref folderPath);
            else if (catalog == StarCatalog.NOMAD)
                return NOMADCatalogue.IsValidCatalogLocation(ref folderPath);
            else if (catalog == StarCatalog.UCAC3)
                return UCAC3Catalogue.IsValidCatalogLocation(ref folderPath);

            return false;
        }

        public bool VerifyCurrentCatalogue()
        {
            if (Catalog == StarCatalog.UCAC2)
            {
                if (!UCAC2Catalogue.IsValidCatalogLocation(ref CatalogLocation))
                    return false;

                if (!UCAC2Catalogue.CheckAndWarnIfNoBSS(CatalogLocation, null))
                    return false;
            }
            else if (Catalog == StarCatalog.NOMAD)
            {
                if (!NOMADCatalogue.IsValidCatalogLocation(ref CatalogLocation))
                    return false;

                // TODO: Check index files??
            }
            else if (Catalog == StarCatalog.UCAC3)
            {
                if (!UCAC3Catalogue.IsValidCatalogLocation(ref CatalogLocation))
                    return false;

            }

            return true;
        }
    }
}
