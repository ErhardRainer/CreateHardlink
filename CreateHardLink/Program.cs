using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;


namespace CreateHardLinkPrG
{
    class Program
    {
        const string quote = "\"";
        const int targetIsAFile = 0;
        const int targetIsADirectory = 1;
        [DllImport("kernel32.dll", EntryPoint = "CreateHardLinkA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern long CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
        static void Main(string[] args)
        {
            if ((args.Length == 1) && (args[0] == "/?"))
            {
                FehlerAusgabe();
            }
            else if ((args.Length >= 2) || (args.Length == 0))
            {
                Console.WriteLine("Create Hardlinks/Softlinks/Reparse Points/lnk-Files");
                string Param1 = string.Empty;
                string Param2 = string.Empty;
                if (args.Length == 0)
                {
                    Console.WriteLine("SourceFile/SourceDirectory/List:");
                    Param1 = Console.ReadLine().ToString();
                    Console.WriteLine("TargetFile/TargetDirectory");
                    Param2 = Console.ReadLine().ToString();
                }
                else
                {
                    Param1 = args[0].ToString();
                    Param2 = args[1].ToString();
                }
                if ((isListFile(Param1) == true) && (isFile(Param2) == false))
                // es handelt sich um eine Listendatei und ein Verzeichnis
                {
                    Console.WriteLine("{0} is a Listfile", Param1);
                    Console.WriteLine("{0} is a Directory", Param2);
                    ListeAuslesen(Param1, Param2,args);
                }
                else if ((isFile(Param1) == true) && (isListFile(Param1) == false) && (isFile(Param2) == false))
                // es handelt sich um eine normale Datei und ein Verzeichnis
                {
                    Console.WriteLine("{0} is a File", Param1);
                    Console.WriteLine("{0} is a Directory", Param2);
                    CreateHardLinkFile2Folder(Param1, Param2, args);
                }
                else if ((isFile(Param1) == false) && (isListFile(Param1) == false))
                // es handelt sich um 2 Verzeichnisse
                {
                    Console.WriteLine("{0} is a Directory", Param1);
                    Console.WriteLine("{0} is a Directory", Param2);
                    CreateJunctionPointFolder2Folder(Param1, Param2, args);
                }
                else if ((isFile(Param1) == true) && (isFile(Param1) == true))
                // es handelt sich um 2 Dateien
                {
                    Console.WriteLine("{0} is a File", Param1);
                    Console.WriteLine("{0} is a File", Param2);
                    CreateConnectionFiles(Param1, Param2, args);
                }
                else
                {
                    FehlerAusgabe();
                }
            }
        Console.ReadKey();
        }

#region Liste_Auslesen
        /// <summary>
        /// Liest die Datei aus und erstellt entsprechende Links
        /// </summary>
        /// <param name="strList"></param>
        /// <param name="TargetDirectory"></param>
        static void ListeAuslesen(string strList, string TargetDirectory, string[] myargs)
        {
            // Auslesen der Datei
            Console.WriteLine("Processing Files of {0}", strList);
            FileStream fs = new FileStream(strList, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            sr.BaseStream.Seek(0, SeekOrigin.Begin);
            while (sr.Peek() > -1)
            {
                string LineFile = sr.ReadLine();
                if (isFile(LineFile) == true)
                // Die Zeile ist eine Datei
                {
                    if (fileexists(LineFile) == true)
                    {
                        CreateHardLinkFile2Folder(LineFile, TargetDirectory, myargs);
                    }
                }
                else
                // Die Zeile ist ein Ordner
                {
                    if (folderexists(LineFile) == true)
                    {
                        CreateJunctionPointFolder2Folder(LineFile, TargetDirectory, myargs);
                    }
                }
            }
            sr.Close();
            fs.Close();
        }
#endregion

# region File2File
        /// <summary>
        /// Stufe 1 WAS FÜR EINE ART?
        /// Ermittelt die Art des Links, der erzeugt werden soll
        /// </summary>
        /// <param name="SourceFile"></param>
        /// <param name="TargetFile"></param>
        /// <param name="myargs"></param>
        static void CreateConnectionFiles(string SourceFile, string TargetFile, string[] myargs)
        {
            if (isParam(myargs, 3, "/s") == true)
            // Create Symbolic Link
            {
            }
            else if (isParam(myargs, 3, "/l") == true)
            // Create LNK-Datei
            {
            }
            else
            // Create HardLink oder Kopie
            {
                CreateHardLinkFile2File(SourceFile, TargetFile, myargs);
            }
        }


        /// <summary>
        /// Stufe 2 HARDLINK: Ermittelt was mit der Datei gemacht werden soll 
        /// überschreiben / überspringen / nächste Datei
        /// </summary>
        /// <param name="SourceFile"></param>
        /// <param name="TargetFile"></param>
        static void CreateHardLinkFile2File(string SourceFile, string TargetFile, string[] myargs)
        {
            if (fileexists(SourceFile) == true) 
            {
                if (fileexists(TargetFile) == true)
                {
                    FileInfo fit = new FileInfo(TargetFile);
                    // Ermitteln, ob die Übergabeparameter ein Überschreiben oder dergleichen vorsehen
                    if ((isParam(myargs, 3, "/o") == true) || (isParam(myargs, 4, "/o") == true))
                    {
                        Console.WriteLine("Are you sure to delete the file {0} ? [y]es | [n]o", TargetFile);
                        var mykey = Console.ReadKey();
                        if (mykey.Key == ConsoleKey.X)
                        {
                            Console.WriteLine("Delete File {0}", TargetFile);
                            fit.Delete();
                            HardLinkORCopy(SourceFile,TargetFile);
                        }
                        else 
                        {
                            Console.WriteLine("Skip File {0}", TargetFile);
                        }
                    }
                    else if ((isParam(myargs, 3, "/n") == true) || (isParam(myargs, 4, "/n") == true))
                    // Ermittelt die nächste freie Datei
                    {
                        TargetFile = findNextFileName(TargetFile);
                        HardLinkORCopy(SourceFile,TargetFile);
                    }
                    else if ((isParam(myargs, 3, "/skip") == true) || (isParam(myargs, 4, "/skip") == true))
                    // Überspringt die Datei
                    {
                        Console.WriteLine("Skip File {0}", TargetFile);
                    }
                }
                else 
                {
                    // Erstellt einen Hardlink oder eine Kopie
                    HardLinkORCopy(SourceFile,TargetFile);
                }
            }
        }

        /// <summary>
        /// Stufe 3 HARDLINK: Erstellt einen HardLink oder eine Kopie
        /// </summary>
        /// <param name="SourceFile"></param>
        /// <param name="TargetFile"></param>
        static void HardLinkORCopy(string SourceFile,string TargetFile)
        {
                    // wenn die Dateien am selben Laufwerk sind Hardlink erstellen
                    if (SourceFile.Substring(0, 1) == TargetFile.Substring(0, 1))
                    {
                        Console.WriteLine("Create Hardlink for '{0}' with '{1}'", SourceFile, TargetFile);
                        CreateHardLink(TargetFile,SourceFile, IntPtr.Zero);
                    }
                    else // Dateien nicht auf der selben Partition
                    {
                        Console.WriteLine("++Copy File form '{0}' to '{1}'", SourceFile, TargetFile);
                        FileInfo fis = new FileInfo(SourceFile);
                        fis.CopyTo(TargetFile);
                    }
        }

        #region Softlink
        static void CreateDirectoryLink(string linkPath, string targetPath)
        {
            if (!CreateSymbolicLink(linkPath, targetPath, targetIsADirectory) || Marshal.GetLastWin32Error() != 0)
            {
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    throw new IOException(exception.Message, exception);
                }
            }
        }

        static void CreateFileLink(string linkPath, string targetPath)
        {
            if (!CreateSymbolicLink(linkPath, targetPath, targetIsAFile))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
        #endregion
        #endregion

#region File2Folder
        /// <summary>
        /// Erstellt von einen Datei eine Kopie oder einen Hardlink 
        /// im Zielverzeichnis mit dem selben Namen
        /// </summary>
        /// <param name="SourceFile"></param>
        /// <param name="TargetDirectory"></param>
        static void CreateHardLinkFile2Folder(string SourceFile, string TargetDirectory, string[] myargs)
        {
            FileInfo fi = new FileInfo(SourceFile);
            if (fi.Exists == true)
            {
                string filename = fi.Name;
                string SourceFileTested = SourceFile;
                // Baut die Zieldatei zusammen
                string TargetFileTested = TargetDirectory + filename;
                CreateConnectionFiles(SourceFileTested, TargetFileTested, myargs);
            }
        }

        #endregion

#region Folder2Folder

        static void CreateJunctionPointFolder2Folder(string strSourceFolder, string strTargetFolder, string[] myargs)
        {
            if ((isParam(myargs, 3, "/subdir") == true) || (isParam(myargs, 4, "/subdir") == true))
            {
                // dann leeres Verzeichnis eine Ebene tiefer erstellen und verlinken
                // Ermitteln des Ordnernamens der Quelle
                DirectoryInfo dis = new DirectoryInfo(strSourceFolder);
                string Foldername = dis.Name;
                if (folderexists(strTargetFolder + Foldername) == true)
                {
                    // Ordner existiert bereits
                    Console.WriteLine("Folder allready exists '{0}'", strTargetFolder + Foldername);
                }
                else
                {
                    Console.WriteLine("Create Junction Point from {0} to {1}", strSourceFolder, strTargetFolder + Foldername);
                    CreateJunctionPoint(strSourceFolder, strTargetFolder + Foldername);
                }
            }
            else
                // dann direkt mit dem Verzeichnis verlinken
            {
                if (folderexists(strTargetFolder) == true)
                {
                    if (isDirectoryEmpty(strTargetFolder) == true)
                    {
                        // Verzeichnis löschen + Junction Point erstellen
                        DirectoryInfo dit = new DirectoryInfo(strTargetFolder);
                        dit.Delete();
                        CreateJunctionPoint(strSourceFolder, strTargetFolder);
                    }
                    else
                    {
                        Console.WriteLine("Folder not empty => Skip Folder '{0}'", strTargetFolder);
                    }
                }
                else
                {
                    // Junction Point erstellen
                    CreateJunctionPoint(strSourceFolder, strTargetFolder);
                }
            }
        }

        /// <summary>
        /// Erstellt einen Junction Point
        /// hierbei darf das Zielverzeichnis noch nicht existieren!
        /// </summary>
        /// <param name="strSourceFolder"></param>
        /// <param name="strTargetfolder"></param>
        static void CreateJunctionPoint(string strSourceFolder, string strTargetfolder)
        {
            // Erstelle einen Junction Point
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C mklink /J " + quote + strTargetfolder + quote + " " + quote + strSourceFolder + quote;
            process.StartInfo = startInfo;
            process.Start();
        }
#endregion

#region isParameter
        /// <summary>
        /// Ermittelt, ob ein bestimmter Übergabeparameter einen bestimmten Wert hat
        /// </summary>
        /// <param name="myargs"></param>
        /// <param name="NrParam"></param>
        /// <param name="CompString"></param>
        /// <returns></returns>
        static Boolean isParam(string[] myargs, int NrParam, string CompString)
        {
            int tmpNrParam = NrParam - 1;
            if (myargs.Count() > tmpNrParam)
            {
                if (myargs[tmpNrParam].ToString() == CompString)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else 
            {
                return false;
            }
        }
#endregion

# region Hilfsfunktionen

        /// <summary>
        /// Prüft, ob der Ordner existiert
        /// </summary>
        /// <param name="strFolder"></param>
        /// <returns></returns>
        static bool folderexists(string strFolder)
        {
            DirectoryInfo di = new DirectoryInfo(strFolder);
            return di.Exists;
        }

        /// <summary>
        /// Prüft, ob die Datei existiert
        /// </summary>
        /// <param name="strFile"></param>
        /// <returns></returns>
        static bool fileexists(string strFile)
        {
            FileInfo fi = new FileInfo(strFile);
            return fi.Exists;
        }

        /// <summary>
        /// Zählt die Datei hoch, bie eine nicht existiert
        /// </summary>
        /// <param name="strFile"></param>
        /// <returns></returns>
        static string findNextFileName(string strFile)
        {
            if (fileexists(strFile) == false)
            { 
                return strFile;
            }
            else 
            {
                string tmpfile = strFile;
                FileInfo fi = new FileInfo(strFile);
                string directorypath = fi.Directory.ToString() + "\\";
                string filenamewithoutextension = Path.GetFileNameWithoutExtension(strFile);
                string extension = fi.Extension;
                int i = 1;
                do
                {
                    tmpfile = directorypath + filenamewithoutextension + i.ToString() + extension;
                } while (fileexists(tmpfile) == true) ;
                return tmpfile;
            }
        }


        /// <summary>
        /// Prüft, ob die Datei eine Listendatei ist 
        /// dies ist der Fall, wenn die Datei in einem TEMP Verzeichnis liegt
        /// </summary>
        /// <param name="strPath"></param>
        /// <returns></returns>
        static bool isListFile(string strPath)
        {
            if (isFile(strPath) == false)
            {
                return false;
            }
            else
            {
                FileInfo fi = new FileInfo(strPath);
                if ((fi.Extension == ".tmp") || (fi.Extension == ".lst"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Prüft, ob es sich um eine Datei handelt
        /// </summary>
        /// <param name="strPath"></param>
        /// <returns></returns>
        static bool isFile(string strPath)
        {
            try
            {
                // get the file attributes for file or directory
                FileAttributes attr = File.GetAttributes(strPath);

                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    return false;
                else
                    return true;
            } 
            catch (FileNotFoundException ex)
            {
                return true;
            }
        }

        /// <summary>
        /// Prüft, ob ein Verzeichnis leer ist
        /// </summary>
        /// <param name="strPath"></param>
        /// <returns></returns>
        static bool isDirectoryEmpty(string strPath)
        {
            return !Directory.EnumerateFileSystemEntries(strPath).Any();
        }

#endregion

#region Fehlerausgabe

        /// <summary>
        /// Fehlerausgabe
        /// </summary>
        static void FehlerAusgabe()
        {
            Console.WriteLine("Sie müssen einerseits die Quelldatei und andererseits das Zielverzeichnis übergeben");
            Console.WriteLine("Beispiele:");
            Console.WriteLine("CreateHardLink C:\\aaa\\test.txt C:\\bbb\\test.txt");
        }
#endregion
    }
}
