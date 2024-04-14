﻿using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace SecEncryptieTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string? KeysFolder { get; set; }
        public string? EncryptedImagesFolder { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ChooseDirectory();
            LoadKeyFolder();
        }
        private void ChooseDirectory()
        {
            System.Windows.MessageBox.Show("Kies een folder voor de sleutels","",MessageBoxButton.OK,MessageBoxImage.Information);
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Selecteer een map voor de sleutels.";
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                KeysFolder = folderBrowserDialog.SelectedPath;
                System.Windows.MessageBox.Show("Gekozen map: " + KeysFolder);

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["KeysFolder"].Value = KeysFolder;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                PopulateListBox(KeysFolder);
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private void SetFolderImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    string selectedFolder = dialog.SelectedPath;

                    EncryptedImagesFolder = selectedFolder;

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["EncryptedImagesFolder"].Value = EncryptedImagesFolder;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");

                    System.Windows.MessageBox.Show($"Selected Folder for Encrypted Images: {selectedFolder}");
                }
            }

        }
        private void SetFolderKeyMenuItem_Click(object sender, RoutedEventArgs e)
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
                PopulateListBox(KeysFolder);
            }
        }
        private void LoadKeyFolder()
        {
            KeysFolder = ConfigurationManager.AppSettings["KeysFolder"];
            EncryptedImagesFolder = ConfigurationManager.AppSettings["EncryptedImagesFolder"];
        }

        #region listBox
        private void PopulateListBox(string directoryPath)
        {
            AeskeyList.Items.Clear();
            try
            {
                string[] files = Directory.GetFiles(directoryPath);
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).ToLower().Contains("key"))
                    {
                        AeskeyList.Items.Add(Path.GetFileName(file));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void keyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AeskeyList.SelectedItem != null)
            {
                string selectedFileName = AeskeyList.SelectedItem.ToString();

                string selectedFilePath = Path.Combine(KeysFolder, selectedFileName);

                System.Windows.MessageBox.Show("Selected file: " + selectedFilePath);
            }
        }
        #endregion listBox
        #region AES
        #region generateAndSaveAesKey
        private void GenerateAESKeys_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            //KeysFolder = folderBrowserDialog.SelectedPath;
            
            string aesKey = GenerateAESKey();
            string aesIV = GenerateAESIV();

            SaveAESKeyToFile(aesKey, aesIV);
            PopulateListBox(KeysFolder);
        }
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

                string keyFilePath = Path.Combine(KeysFolder, keyFilename);
                string ivFilePath = Path.Combine(KeysFolder, ivFilename);

                try
                {
                    File.WriteAllText(keyFilePath, aesKey);
                    File.WriteAllText(ivFilePath, aesIV);
                    PopulateListBox(KeysFolder);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Oei, het is niet gelukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                System.Windows.MessageBox.Show($"Mooi, de AES keys zijn succesvol opgeslagen", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Geef eerst een naam in voor je sleutel", "How jong, wacht is", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


        }
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
        #endregion generateAndSaveAesKey
        private void EncryptImageWithAES_Click(object sender, RoutedEventArgs e)
        {
            if (EncryptedImagesFolder == null)
            {
                System.Windows.MessageBox.Show("Kies eerst via bovenstaand menu een folder om je bestand in op te slaan", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (AeskeyList.SelectedItem == null)
                {
                    System.Windows.MessageBox.Show("Selecteer eerst een AES-sleutel in de lijst.", "Melding", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                System.Windows.MessageBox.Show("Kies de foto om te encrypteren.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";
                DialogResult result = openFileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    DisplayImage(openFileDialog.FileName);
                    string imagePath = openFileDialog.FileName;

                    // Get the selected key file from the list box
                    string selectedKeyFileName = AeskeyList.SelectedItem.ToString();
                    string aesKeyPath = Path.Combine(KeysFolder, selectedKeyFileName);
                    string aesIVPath = aesKeyPath.Substring(0, aesKeyPath.Length - 11) + "_AesIV.txt";
                    string aesKey = File.ReadAllText(aesKeyPath);
                    string aesIV = File.ReadAllText(aesIVPath);

                    EncryptImageUsingAES(imagePath, aesKey, aesIV);

                    ClearImage(openFileDialog.FileName);
                }
            }
        }
        private void DecryptImageWithAES_Click(object sender, RoutedEventArgs e)
        {
            if (AeskeyList.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Selecteer eerst een AES-sleutel in de lijst.", "Melding", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            System.Windows.MessageBox.Show("Kies de foto om te decrypteren.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt";
            DialogResult result = openFileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string encryptedImagePath = openFileDialog.FileName;

                string selectedKeyFileName = AeskeyList.SelectedItem.ToString();
                string aesKeyPath = Path.Combine(KeysFolder, selectedKeyFileName);
                string aesIVPath = aesKeyPath.Substring(0, aesKeyPath.Length - 11) + "_AesIV.txt";
                string aesKey = File.ReadAllText(aesKeyPath);
                string aesIV = File.ReadAllText(aesIVPath);

                DecryptImageUsingAES(encryptedImagePath, aesKey, aesIV);
            }
        }

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

                    string encryptedImagePath = Path.Combine(EncryptedImagesFolder, Path.GetFileNameWithoutExtension(imagePath) + "_encrypted" + Path.GetExtension(".txt"));
                    File.WriteAllText(encryptedImagePath, encryptedImageBase64);

                    System.Windows.MessageBox.Show("Afbeelding is succesvol geencrypteerd.", "Let's gooo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout bij het versleutelen van de afbeelding: {ex.Message}", "Kutzooi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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
                    aesAlg.Key = Convert.FromBase64String(key);
                    aesAlg.IV = Convert.FromBase64String(iv);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
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
                    DisplayImage(decryptedImagePath);
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
            if (string.IsNullOrEmpty(KeysFolder))
            {
                System.Windows.MessageBox.Show("Selecteer eerst een map voor de RSA keys.", "How jong, wacht is", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string publicFilename = "RSAPublicKey";
            string privateFilename = "RSAPrivateKey";

            if (string.IsNullOrEmpty(TxtKeyNaam.Text))
            {
                System.Windows.MessageBox.Show("Geef eerst een naam in voor je sleutel", "How jong, wacht is", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            publicFilename = TxtKeyNaam.Text;
            privateFilename = TxtKeyNaam.Text;
            publicFilename += "_RsaPublic.xml";
            privateFilename += "_RsaPrivate.xml";
            
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
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
                    return;
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
                byte[] encryptedData = rsa.Encrypt(imageData, false);
        
                // Show Dialog to Get output file name
                var dialog = new System.Windows.Forms.SaveFileDialog();
                dialog.Filter = "Text files (*.txt)|*.txt";
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    string encryptedFileName = dialog.FileName;
                    File.WriteAllText(encryptedFileName, Convert.ToBase64String(encryptedData));

                    System.Windows.MessageBox.Show($"Key is succesvol versleuteld met RSA en opgeslagen in {encryptedFileName}.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
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

                byte[] encryptedImageData = Convert.FromBase64String(File.ReadAllText(encryptedImagePath));
                byte[] decryptedData = rsa.Decrypt(encryptedImageData, false);

                // Show save file dialog to choose filename to save decrypted AES key
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text files (*.txt)|*.txt";
                DialogResult result = saveFileDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string decryptedFileName = saveFileDialog.FileName;
                    File.WriteAllText(decryptedFileName, Convert.ToBase64String(decryptedData));

                    System.Windows.MessageBox.Show($"Key is succesvol ontsleuteld met RSA en opgeslagen in {decryptedFileName}.", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Fout bij het ontsleutelen van de key met RSA: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion RSA

        #region image

        private void DisplayImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.EndInit();
                    imgDisplayed.Source = bitmap;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void ClearImage(string imagePath)
        {
            imgDisplayed.Source = null;
        }

        #endregion image
    }
}