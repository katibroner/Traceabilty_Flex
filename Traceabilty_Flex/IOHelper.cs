//-----------------------------------------------------------------------
// <copyright file="IOHelper.cs" company="ASM Assembly Systems GmbH & Co. KG">
//     Copyright (c) ASM Assembly Systems GmbH & Co. KG. All rights reserved.
// </copyright>
// <email>oib-support.siplace@asmpt.com</email>
// <summary>
//    This code is part of the OIB SDK. 
//    Use and redistribution is free without any warranty. 
// </summary>
//-----------------------------------------------------------------------

#region using

using System;
using System.IO;
using System.Xml.Serialization;

#endregion

namespace TraceabilityTestGui
{
    public class IOHelper<T>
    {
        /// <summary>
        /// Gets the name of the time stamped file name.
        /// </summary>
        /// <returns></returns>
        public static string GetTimeStampedName()
        {
            string name = "";
            name += DateTime.Now.ToString("yyyyMMddHHmmssff");
            name += "_";
            name += typeof(T).ToString();
            name += ".xml";
            return name;
        }


        /// <summary>
        /// Writes the specified serializable class.
        /// </summary>
        /// <param name="report">The report.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public static bool Write(T report, string fileName)
        {
            try
            {
                Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                using (stream)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(stream, report);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Writes the class content to pathname file.
        /// </summary>
        /// <param name="serializableClass">The serializable class.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static bool WriteToPath(T serializableClass, string path)
        {
            string fileName = GetTimeStampedName();
            string newPath = Path.Combine(path, fileName);
            return Write(serializableClass, newPath);
        }

        /// <summary>
        /// Writes to path.
        /// </summary>
        /// <param name="serializableClass">The serializable class.</param>
        /// <param name="hint">The hint.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static bool WriteToPath(T serializableClass, string hint, string path)
        {
            string fileName = GetTimeStampedName();
            string newPath = Path.Combine(path, fileName);
            return Write(serializableClass, newPath);
        }
    }

}