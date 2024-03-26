using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Forms;

namespace SecEncryptieTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string? KeysFolder { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            LoadKeyFolder();
        }

        private void LoadKeyFolder()
        {
            KeysFolder = ConfigurationManager.AppSettings["KeysFolder"];
        }

        private void SetKeysFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                KeysFolder = folderBrowserDialog.SelectedPath;

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["KeysFolder"].Value = KeysFolder;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        #region AES

        private void GenerateAESKeys_Click(object sender, RoutedEventArgs e)
        {
            //byte[] aesKey = GenerateAESKey();
            //byte[] aesIV = GenerateAESIV();
            string aesKey = GenerateAESKey();
            string aesIV = GenerateAESIV();

            SaveAESKeyToFile(aesKey, aesIV);
        }

        //private void SaveAESKeyToFile(byte[] aesKey, byte[] aesIV)
        private void SaveAESKeyToFile(string aesKey, string aesIV)
        {
            if (string.IsNullOrEmpty(KeysFolder))
            {
                System.Windows.MessageBox.Show("Selecteer eerst een map voor de AES keys.", "How jong, wacht is", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string keyFilename = "AESKey";
            string ivFilename = "AESIV";

            if (TxtKeyNaam.Text != string.Empty)
            {
                keyFilename = TxtKeyNaam.Text;
                ivFilename = TxtKeyNaam.Text;
                keyFilename += "_AesKey.txt";
                ivFilename += "_AesIV.txt";
            }

            string keyFilePath = Path.Combine(KeysFolder, keyFilename);
            string ivFilePath = Path.Combine(KeysFolder, ivFilename);

            try
            {
                //File.WriteAllBytes(keyFilePath, aesKey);
                //File.WriteAllBytes(ivFilePath, aesIV);
                File.WriteAllText(keyFilePath, aesKey);
                File.WriteAllText(ivFilePath, aesIV);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Oei, het is niet gelukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            System.Windows.MessageBox.Show($"Mooi, de AES keys zijn succesvol opgeslagen", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        //private byte[] GenerateAESKey()
        //{
        //    using (Aes aesAlg = Aes.Create())
        //    {
        //        aesAlg.GenerateKey();
        //        return aesAlg.Key;
        //    }
        //}

        //private byte[] GenerateAESIV()
        //{
        //    using (Aes aesAlg = Aes.Create())
        //    {
        //        aesAlg.GenerateIV();
        //        return aesAlg.IV;
        //    }
        //}
        private string GenerateAESKey()
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateKey();
                return Convert.ToBase64String(aesAlg.Key);
            }
        }

        private string GenerateAESIV()
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateIV();
                return Convert.ToBase64String(aesAlg.IV);
            }
        }

        private void EncryptImageWithAES_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Kies de foto om te encrypteren.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string imagePath = openFileDialog.FileName;

                System.Windows.MessageBox.Show("Kies nu de AES Key", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);

                openFileDialog.Filter = "Text files (*.txt)|*.txt";
                result = openFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string aesKeyPath = openFileDialog.FileName;
                    string aesIVPath = aesKeyPath.Substring(0, aesKeyPath.Length - 11) + "_AesIV.txt";
                    //System.Windows.MessageBox.Show("Kies nu de AES IV", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
                    //result = openFileDialog.ShowDialog();
                    //if (result == System.Windows.Forms.DialogResult.OK)
                    //{
                    //string aesIVPath = openFileDialog.FileName;
                    //byte[] aesKey = File.ReadAllBytes(aesKeyPath);
                    //byte[] aesIV = File.ReadAllBytes(aesIVPath);
                    string aesKey = File.ReadAllText(aesKeyPath);
                    string aesIV = File.ReadAllText(aesIVPath);

                    EncryptImageUsingAES(imagePath, aesKey, aesIV);
                    //}
                }
            }
        }

        private void DecryptImageWithAES_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Kies de foto om te decrypteren.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg;*.txt";
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string encryptedImagePath = openFileDialog.FileName;

                System.Windows.MessageBox.Show("Kies nu de AES Key", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);

                openFileDialog.Filter = "Text files (*.txt)|*.txt";
                result = openFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string aesKeyPath = openFileDialog.FileName;
                    string aesIVPath = aesKeyPath.Substring(0, aesKeyPath.Length - 11) + "_AesIV.txt";
                    //System.Windows.MessageBox.Show("Kies nu de AES IV", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
                    //result = openFileDialog.ShowDialog();
                    //if (result == System.Windows.Forms.DialogResult.OK)
                    //{
                    //string aesIVPath = openFileDialog.FileName;

                        //byte[] aesKey = File.ReadAllBytes(aesKeyPath);
                        //byte[] aesIV = File.ReadAllBytes(aesIVPath);
                        string aesKey = File.ReadAllText(aesKeyPath);
                        string aesIV = File.ReadAllText(aesIVPath);

                        DecryptImageUsingAES(encryptedImagePath, aesKey, aesIV);
                    //}
                }
            }
        }

        //private void EncryptImageUsingAES(string imagePath, byte[] key, byte[] iv)
        private void EncryptImageUsingAES(string imagePath, string key, string iv)
        {
            if (key == null || iv == null)
            {
                System.Windows.MessageBox.Show("AES-sleutel of IV niet geladen.", "Waarschuwing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(imagePath))
            {
                System.Windows.MessageBox.Show("Afbeelding niet gevonden op het opgegeven pad.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            byte[] imageData = File.ReadAllBytes(imagePath);

            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    //aesAlg.Key = key;
                    //aesAlg.IV = iv;
                    aesAlg.Key = Convert.FromBase64String(key);
                    aesAlg.IV = Convert.FromBase64String(iv);

                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    byte[] encryptedData;
                    string encryptedImageBase64;
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(imageData, 0, imageData.Length);
                            csEncrypt.FlushFinalBlock();
                            encryptedData = msEncrypt.ToArray();
                            encryptedImageBase64 = Convert.ToBase64String(encryptedData);
                        }
                    }

                    //string encryptedImagePath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath) + "_encrypted" + Path.GetExtension(imagePath));
                    string encryptedImagePath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath) + "_encrypted" + Path.GetExtension(".txt"));
                    //File.WriteAllBytes(encryptedImagePath, encryptedData);
                    File.WriteAllText(encryptedImagePath, encryptedImageBase64);

                    System.Windows.MessageBox.Show("Afbeelding is succesvol geencrypteerd.", "Let's gooo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout bij het versleutelen van de afbeelding: {ex.Message}", "Kutzooi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private void DecryptImageUsingAES(string imagePath, byte[] key, byte[] iv)
        private void DecryptImageUsingAES(string imagePath, string key, string iv)
        {
            if (key == null || iv == null)
            {
                System.Windows.MessageBox.Show("AES key of IV niet geladen.", "Oei", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(imagePath))
            {
                System.Windows.MessageBox.Show("Geen foto gevonden daar.", "Shit", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    //aesAlg.Key = key;
                    //aesAlg.IV = iv;
                    aesAlg.Key = Convert.FromBase64String(key);
                    aesAlg.IV = Convert.FromBase64String(iv);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    //byte[] encryptedImageData = File.ReadAllBytes(imagePath);
                    string base64EncryptedImageData = File.ReadAllText(imagePath);
                    byte[] encryptedImageData = Convert.FromBase64String(base64EncryptedImageData);

                    byte[] decryptedData;
                    using (MemoryStream msDecrypt = new MemoryStream())
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                        {
                            csDecrypt.Write(encryptedImageData, 0, encryptedImageData.Length);
                            csDecrypt.FlushFinalBlock();
                            decryptedData = msDecrypt.ToArray();
                        }
                    }
                    imagePath = imagePath.Substring(0, imagePath.Length - 4) + ".jpg";
                    string decryptedImagePath = Path.Combine(Path.GetDirectoryName(imagePath), "decrypted_" + Path.GetFileName(imagePath));
                    File.WriteAllBytes(decryptedImagePath, decryptedData);

                    System.Windows.MessageBox.Show("Afbeelding is succesvol gedecrypteerd.", "Goeie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Geen feest", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion AES

        #region RSA

        private void GenerateRSAKeys_Click(object sender, RoutedEventArgs e)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                if (string.IsNullOrEmpty(KeysFolder))
                {
                    System.Windows.MessageBox.Show("Selecteer eerst een map voor de RSA keys.", "How jong, wacht is", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string publicFilename = "RSAPublicKey";
                string privateFilename = "RSAPrivateKey";

                if (TxtKeyNaam.Text != string.Empty)
                {
                    publicFilename = TxtKeyNaam.Text;
                    privateFilename = TxtKeyNaam.Text;
                    publicFilename += "_RsaPublic.xml";
                    privateFilename += "_RsaPrivate.xml";
                }

                try
                {
                    // public key
                    string publicKeyPath = Path.Combine(KeysFolder, publicFilename);
                    string publicKeyXml = rsa.ToXmlString(false);
                    File.WriteAllText(publicKeyPath, publicKeyXml);

                    // private key
                    string privateKeyPath = Path.Combine(KeysFolder, privateFilename);
                    string privateKeyXml = rsa.ToXmlString(true);
                    File.WriteAllText(privateKeyPath, privateKeyXml);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Oei, het is niet gelukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                System.Windows.MessageBox.Show("Noice, RSA keys succesvol opgeslagen.", "Feest", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EncryptAESWithRSA_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Kies de key om te encrypteren.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt";
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string imagePath = openFileDialog.FileName;

                System.Windows.MessageBox.Show("Kies nu de RSA Public Key", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
                openFileDialog.Filter = "XML files (*.xml)|*.xml";
                result = openFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string publicKeyPath = openFileDialog.FileName;
                    EncryptAESUsingRSA(imagePath, publicKeyPath);
                }
            }
        }

        private void DecryptAESWithRSA_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Kies de key om te decrypteren.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt";
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string imagePath = openFileDialog.FileName;
                openFileDialog.Filter = "XML files (*.xml)|*.xml";

                System.Windows.MessageBox.Show("Kies nu de RSA Private Key", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
                result = openFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string privateKeyPath = openFileDialog.FileName;
                    DecryptAESUsingRSA(imagePath, privateKeyPath);
                }
            }
        }

        private void EncryptAESUsingRSA(string imagePath, string publicKeyPath)
        {
            if (!File.Exists(publicKeyPath))
            {
                System.Windows.MessageBox.Show("RSA public key niet gevonden op het opgegeven pad.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(imagePath))
            {
                System.Windows.MessageBox.Show("Afbeelding niet gevonden op het opgegeven pad.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string publicKeyXml = File.ReadAllText(publicKeyPath);
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKeyXml);

                byte[] imageData = File.ReadAllBytes(imagePath);
                byte[] encryptedData = rsa.Encrypt(imageData, true);
                string encryptedImagePath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath) + "_encrypted_RSA" + Path.GetExtension(imagePath));
                File.WriteAllBytes(encryptedImagePath, encryptedData);

                System.Windows.MessageBox.Show("Key is succesvol versleuteld met RSA en opgeslagen.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout bij het versleutelen van de key met RSA: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecryptAESUsingRSA(string encryptedImagePath, string privateKeyPath)
        {
            if (!File.Exists(privateKeyPath))
            {
                System.Windows.MessageBox.Show("RSA private sleutel niet gevonden op het opgegeven pad.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(encryptedImagePath))
            {
                System.Windows.MessageBox.Show("Versleutelde key niet gevonden op het opgegeven pad.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string privateKeyXml = File.ReadAllText(privateKeyPath);
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(privateKeyXml);

                byte[] encryptedImageData = File.ReadAllBytes(encryptedImagePath);
                byte[] decryptedData = rsa.Decrypt(encryptedImageData, true);

                string decryptedImagePath = Path.Combine(Path.GetDirectoryName(encryptedImagePath), "decrypted_RSA_" + Path.GetFileName(encryptedImagePath));
                File.WriteAllBytes(decryptedImagePath, decryptedData);

                System.Windows.MessageBox.Show("Key is succesvol ontsleuteld met RSA en opgeslagen.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout bij het ontsleutelen van de key met RSA: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion RSA
    }
}