using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Windows.Forms.DataVisualization.Charting;
using System.Diagnostics;
using MoreLinq;

namespace MerkleTreeImplmentation
{
    public partial class Form1 : Form
    {

        string _folderPath = @"C:\Users\pratik\Desktop\CS579\lectureNotes";
        List<string> _lstFileNames = null;
        List<string> _lstFileHashes = null;
        Stopwatch sw = null;

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Computes time required for variable length messages. 
        /// In this particular instance this is reading each file and appending the contents
        /// </summary>
        private void ComputeHashingForVariableMessageLength(string dirName)
        {
            SortedDictionary<int, long> _dc = new SortedDictionary<int, long>();
            List<byte[]> _lstFileConents = new List<byte[]>();
            try
            {
                //Get the file names in the directory.
                if (_lstFileNames.Count.Equals(0))
                    ReadDirectory(dirName);

                foreach (string fName in _lstFileNames)
                {
                    //Read contents of the file
                    _lstFileConents.Add(ReadFile(fName));
                    //append to previous contents
                    byte[] _arr = _lstFileConents.SelectMany(a => a).ToArray();
                    //start timer
                    sw.Start();
                    //compute hash of the content
                    CreateHashOfContent(_arr);
                    //stop timer
                    sw.Stop();
                    //get time elapsed for computing x bytes.
                    _dc.Add(_arr.Length, sw.ElapsedMilliseconds);
                }

                PlotGraph(_dc);
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        /// <summary>
        /// Read the file names in the directory
        /// </summary>
        private void ReadDirectory(string directoryName)
        {
            try
            {
                foreach (string strFileName in Directory.EnumerateFiles(directoryName, "*.*", SearchOption.AllDirectories))
                    _lstFileNames.Add(strFileName);
                _lstFileNames.Sort();
                string _strOutput = "Files will be processed in below order:";
                foreach (string fName in _lstFileNames)
                {
                    _strOutput = $"{_strOutput}{Environment.NewLine}{fName}{Environment.NewLine}";
                }
                txtOutput.Text = _strOutput;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Reads the bytes(contents) of the file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Byte array of the file</returns>
        private byte[] ReadFile(string fileName)
        {
            byte[] _fileBytes = File.ReadAllBytes(fileName);
            return _fileBytes;
        }

        /// <summary>
        /// Creates a SHA256 hash from the bytes
        /// </summary>
        /// <param name="fileContent"></param>
        /// <returns>SHA256 Hex code in string</returns>
        private string CreateHashOfContent(byte[] fileContent)
        {
            using (SHA256CryptoServiceProvider shaProvider = new SHA256CryptoServiceProvider())
            {
                byte[] hash = shaProvider.ComputeHash(fileContent);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        /// <summary>
        /// Creates and generates the plot from the sorted dictionary
        /// </summary>
        /// <param name="map"></param>
        private void PlotGraph(SortedDictionary<int, long> map)
        {

            foreach (KeyValuePair<int, long> kvp in map)
            {
                chart1.Series["Series1"].Points.AddXY(kvp.Key, kvp.Value);
            }

            chart1.Series["Series1"].ChartType = SeriesChartType.FastLine;
            chart1.Series["Series1"].Color = Color.Red;
            chart1.Series["Series1"].SmartLabelStyle.Enabled = true;
            chart1.ChartAreas[0].AxisX.Title = "Length of messages";
            chart1.ChartAreas[0].AxisY.Title = "Time for hashing";
        }

        /// <summary>
        /// Function to compute hash value for the root node of Merkle tree when leaf nodes are file contents directly
        /// </summary>
        /// <param name="dirName">Directory path on which Merkle tree needs to be computed</param>
        /// <returns>String representation of hex value</returns>
        private void ComputeMerkleTreeOverFiles(string dirName)
        {
            string _strRtnVal = string.Empty;
            List<byte[]> _lstFileConents = new List<byte[]>();
            try
            {
                sw.Restart();
                //Read directory
                if (_lstFileNames.Count.Equals(0))
                    ReadDirectory(dirName);
                //Read file contents
                foreach (string fName in _lstFileNames)
                    _lstFileConents.Add(ReadFile(fName));

                //if there is odd number of leaf nodes, add last node again.
                if (!(_lstFileConents.Count % 2).Equals(0))
                    _lstFileConents.Add(_lstFileConents.Last());

                _lstFileHashes = new List<string>();

                //Calculate hash using two files appended at a time.
                _lstFileConents
                    .Batch(2)
                    .ForEach((group) =>
                    {
                        byte[] a = group.First();
                        byte[] b = group.Last();
                        byte[] _arr = new byte[a.Length + b.Length];
                        a.CopyTo(_arr, 0);
                        b.CopyTo(_arr, a.Length);
                        _lstFileHashes.Add(CreateHashOfContent(_arr));
                    });
                //Once the leaf nodes are processed in to hash, compute merkle tree over hash
                txtOutput.Text += $"{Environment.NewLine}{ ComputeMerkleTree(_lstFileHashes)}{Environment.NewLine}";
                sw.Stop();
                txtOutput.Text += $"{Environment.NewLine}Time taken for Merkle tree root when started with files {sw.ElapsedMilliseconds}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            //return _strRtnVal;
        }

        /// <summary>
        /// Function to compute hash value for the root node of Merkle tree when leaf nodes are hash values of file
        /// </summary>
        /// <param name="dirName">Directory path on which Merkle tree needs to be computed</param>
        /// <returns>String representation of hex value</returns>
        private void ComputeMerkleTreeOverFileHash(string dirName)
        {
            List<byte[]> _lstFileConents = new List<byte[]>();
            try
            {
                sw.Restart();
                //Read file names
                if (_lstFileNames.Count.Equals(0))
                    ReadDirectory(dirName);
                //Read file contents
                foreach (string fName in _lstFileNames)
                    _lstFileConents.Add(ReadFile(fName));

                _lstFileHashes = new List<string>();

                //Calculated hash of each file as leaf node
                _lstFileConents
                    .ForEach((group) =>
                    {
                        _lstFileHashes.Add(CreateHashOfContent(group));
                    });

                //Compute Merkle tree hash
                txtOutput.Text += $"{Environment.NewLine}{ ComputeMerkleTree(_lstFileHashes)}{Environment.NewLine}";
                sw.Stop();
                txtOutput.Text += $"{Environment.NewLine}Time taken for Merkle tree root when started with files {sw.ElapsedMilliseconds}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Function to compute hash value for the root note of Merkle tree.
        /// </summary>
        /// <param name="lstHashes"></param>
        /// <returns></returns>
        private string ComputeMerkleTree(List<string> lstHashes)
        {
            try
            {
                //In case tree has odd number of leaf nodes, add last again to tree.
                if (!(lstHashes.Count % 2).Equals(0))
                    lstHashes.Add(lstHashes.Last());

                //Get two nodes and compute there hash after concatination.
                List<string> _lst = new List<string>();
                lstHashes
                    .Batch(2)
                    .ForEach((group) =>
                    {
                        _lst.Add(CreateHashOfContent(string.Format($"{group.First()}{group.Last()}").Select(Convert.ToByte).ToArray()));
                    });

                //Repeat the process till there is only one node left.
                if (_lst.Count.Equals(1))
                    return _lst.First();
                else
                    return ComputeMerkleTree(_lst);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return "";
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            DialogResult _result = folderBrowserDialog1.ShowDialog();
            if (_result.Equals(DialogResult.OK))
            {
                _folderPath = folderBrowserDialog1.SelectedPath;
                _lstFileHashes = new List<string>();
                _lstFileNames = new List<string>();
                sw = new Stopwatch();
                ComputeHashingForVariableMessageLength(_folderPath);
                ComputeMerkleTreeOverFiles(_folderPath);
                ComputeMerkleTreeOverFileHash(_folderPath);
            }
        }
    }
}
