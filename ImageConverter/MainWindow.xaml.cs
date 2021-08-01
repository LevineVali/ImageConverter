using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Threading;
using System.Drawing;

using Brushes = System.Windows.Media.Brushes;
using Timer = System.Timers.Timer;
using System.Timers;

namespace ImageConverter
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ImageFormat originFormat;
        private ImageFormat targetFormat;
        private string currentDirectory;
        private string[] subDirectorys;
        private List<string> imageFiles;

        #region Counters for each Format for each Thread

        private int bmp = 0;
        private int emf = 0;
        private int exif = 0;
        private int icon = 0;
        private int jpg = 0;
        private int png = 0;
        private int tiff = 0;
        private int wmf = 0;

        private int amount = 0;
        private int countAllCurrent = 0;
        private int countAllPast = 0;
        #endregion

        private DateTime startTime;
        private Timer timer;

        private MessageWindow loadingWindow;
        private MessageWindow messageWindow;

        private int maxThreads = 0;
        private int finishedThreads = 0;

        private bool? delete;
        private bool? subfolder;

        public delegate void Del(List<string> files);

        public MainWindow()
        {
            InitializeComponent();
            FillFormatList();

            originFormat = ImageFormat.JPG;
            targetFormat = ImageFormat.PNG;

            // get currentDirectory
            currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            delete = true;
            subfolder = false;

            subDirectorys = new string[0];
        }

        private void FillFormatList()
        {
            for (int i = 0; i < (int)ImageFormat.ALL + 1; i++)
            {
                Label chosen = new Label();
                chosen.Content = (ImageFormat)i;
                chosen.Foreground = Brushes.White;
                cb_ChosenFormatToConvert.Items.Add(chosen);

                if (i < (int)ImageFormat.ALL)
                {
                    Label target = new Label();
                    target.Content = (ImageFormat)i;
                    target.Foreground = Brushes.White;
                    cb_TargetFormat.Items.Add(target);
                }
            }

            cb_ChosenFormatToConvert.SelectedIndex = (int)ImageFormat.JPG;
            cb_TargetFormat.SelectedIndex = (int)ImageFormat.PNG;
        }

        private void buttonClick(object sender, RoutedEventArgs e)
        {
            // teste stuff per buttondruck
        }

        public void ConvertImage(string imagename, System.Drawing.Imaging.ImageFormat targetFormat, bool? delete)
        {
            // get all datetime information of the image
            DateTime creationTime = File.GetCreationTime(imagename);
            DateTime creationTimeUtc = File.GetCreationTimeUtc(imagename);
            DateTime lastAccessTime = File.GetLastAccessTime(imagename);
            DateTime lastAccessTimeUtc = File.GetLastAccessTimeUtc(imagename);
            DateTime lastWriteTime = File.GetLastWriteTime(imagename);
            DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(imagename);

            string[] splits = imagename.Split('.');
            string path = splits[0];
            for (int i = 1; i < splits.Length - 1; i++)
            {
                path += "." + splits[i];
            }
            string oldFileEnding = "." + splits[splits.Length - 1];
            splits = path.Split('\\');
            path = "";
            for (int i = 0; i < splits.Length - 1; i++)
            {
                path += splits[i] + "\\";
            }

            string nameOnly = splits[splits.Length - 1];
            string newFileEnding = "." + targetFormat.ToString().ToLower();
            string deleteName = "Original-";

            // rename file
            File.Move(path + nameOnly + oldFileEnding, path + deleteName + nameOnly + oldFileEnding);
            Bitmap image = new Bitmap(path + deleteName + nameOnly + oldFileEnding);
            image.Save(path + nameOnly + newFileEnding, targetFormat);

            // set all datetime information
            File.SetCreationTime(path + nameOnly + newFileEnding, creationTime);
            File.SetCreationTimeUtc(path + nameOnly + newFileEnding, creationTimeUtc);
            File.SetLastAccessTime(path + nameOnly + newFileEnding, lastAccessTime);
            File.SetLastAccessTimeUtc(path + nameOnly + newFileEnding, lastAccessTimeUtc);
            File.SetLastWriteTime(path + nameOnly + newFileEnding, lastWriteTime);
            File.SetLastWriteTimeUtc(path + nameOnly + newFileEnding, lastWriteTimeUtc);

            image.Dispose();
            if (delete == true)
                File.Delete(path + deleteName + nameOnly + oldFileEnding);
        }

        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            // create new FileDialog
            var dialog = new CommonOpenFileDialog();

            // set settings of the FileDialog
            dialog.Title = "Image Converter - Choose Folder";
            dialog.IsFolderPicker = true;
            dialog.InitialDirectory = currentDirectory;

            // open FileDialog and if a folder is choosen
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                currentDirectory = dialog.FileName;

                b_ChooseFolder.Content = currentDirectory;
                b_ChooseFolder.ToolTip = currentDirectory;

                b_Convert.IsEnabled = true;
            }
        }

        private void SwitchFormats(object sender, RoutedEventArgs e)
        {
            ImageFormat origin = originFormat;
            ImageFormat target = targetFormat;

            cb_ChosenFormatToConvert.SelectedIndex = (int)target;
            cb_TargetFormat.SelectedIndex = (int)origin;

            originFormat = target;
            targetFormat = origin;
        }

        private void button_Convert(object sender, RoutedEventArgs e)
        {
            if (subfolder == true)
            {
                // get all subfolders
                subDirectorys = Directory.GetDirectories(currentDirectory, "*", SearchOption.AllDirectories);
            }

            // reset counters
            bmp = 0;
            emf = 0;
            exif = 0;
            icon = 0;
            jpg = 0;
            png = 0;
            tiff = 0;
            wmf = 0;
            amount = 0;
            countAllCurrent = 0;
            countAllPast = 0;

            // new list of strings
            imageFiles = new List<string>();

            for (int j = 0; j < subDirectorys.Length + 1; j++)
            {
                string[] files = new string[0];

                // get all files
                if (j == 0)
                    files = Directory.GetFiles(currentDirectory);
                else
                    files = Directory.GetFiles(subDirectorys[j - 1]);

                string[] splits;
                // get all images
                for (int i = 0; i < files.Length; i++)
                {
                    splits = files[i].Split('.');

                    switch (splits[splits.Length - 1].ToUpper())
                    {
                        case "BMP":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.BMP || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "EMF":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.EMF || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "EXIF":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.EXIF || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "ICO":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.ICO || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "JPG":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.JPG || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "JPEG":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.JPG || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "JPG_LARGE":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.JPG || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "PNG":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.PNG || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "TIFF":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.TIFF || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                        case "WMF":
                            imageFiles.Add(files[i]);
                            if (originFormat == ImageFormat.WMF || originFormat == ImageFormat.ALL)
                                amount++;
                            continue;
                    }
                }
            }

            Label message = new Label();
            message = CreateNewLabel(message);

            messageWindow = new MessageWindow();
            messageWindow.dp_Messages.Children.Clear();

            if (imageFiles.Count == 0)
            {
                message.Content = "No Images found to convert";
                messageWindow.dp_Messages.Children.Add(message);
                messageWindow.ShowDialog();
                return;
            }

            Thread loadingThread = new Thread(new ThreadStart(OpenLoadingWindow));
            loadingThread.SetApartmentState(ApartmentState.STA);
            loadingThread.IsBackground = true;
            loadingThread.Start();

            b_ChooseFolder.IsEnabled = false;
            b_Convert.IsEnabled = false;

            maxThreads = 0;
            finishedThreads = 0;

            // split files in smaller lists for multithreading and faster working
            List<string> filesOne = new List<string>();
            List<string> filesTwo = new List<string>();
            List<string> filesThree = new List<string>();
            List<string> filesFour = new List<string>();

            for (int i = 0; i < imageFiles.Count; i++)
            {
                if (i < imageFiles.Count)
                {
                    if (i == 0)
                        maxThreads++;

                    filesOne.Add(imageFiles[i]);
                    i++;
                }
                else
                    break;

                if (i < imageFiles.Count)
                {
                    if (i == 1)
                        maxThreads++;

                    filesTwo.Add(imageFiles[i]);
                    i++;
                }
                else
                    break;

                if (i < imageFiles.Count)
                {
                    if (i == 2)
                        maxThreads++;

                    filesThree.Add(imageFiles[i]);
                    i++;
                }
                else
                    break;

                if (i < imageFiles.Count)
                {
                    if (i == 3)
                        maxThreads++;

                    filesFour.Add(imageFiles[i]);
                }
                else
                    break;
            }

            startTime = DateTime.Now;
            SetTimer();

            // creat a delegate for each list that isnt empty
            if (filesOne.Count == 0 && filesTwo.Count == 0 && filesThree.Count == 0 && filesFour.Count == 0)
            {
                while (loadingWindow == null) { }
                CloseLoadingWindow(loadingWindow);
                ShowResult();
            }
            else
            {
                if (filesOne.Count != 0)
                {
                    Del con = Convert;
                    IAsyncResult ar = con.BeginInvoke(filesOne, ThreadFinished, null);
                }
                if (filesTwo.Count != 0)
                {
                    Del con = Convert;
                    IAsyncResult ar = con.BeginInvoke(filesTwo, ThreadFinished, null);
                }
                if (filesThree.Count != 0)
                {
                    Del con = Convert;
                    IAsyncResult ar = con.BeginInvoke(filesThree, ThreadFinished, null);
                }
                if (filesFour.Count != 0)
                {
                    Del con = Convert;
                    IAsyncResult ar = con.BeginInvoke(filesFour, ThreadFinished, null);
                }
            }
        }

        private void SetTimer()
        {
            timer = new Timer(1000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (loadingWindow != null)
                {
                    loadingWindow.UpdateTime(DateTime.Now - startTime, "busy for:", false);
                }
            });
        }

        private void ThreadFinished(IAsyncResult result)
        {
            finishedThreads++;

            if (finishedThreads == maxThreads)
                ThreadShowResult();
        }

        private void ShowResult()
        {
            timer.Stop();
            timer.Dispose();

            b_ChooseFolder.IsEnabled = true;
            b_Convert.IsEnabled = true;

            Label message = new Label();
            message = CreateNewLabel(message);
            messageWindow = new MessageWindow();
            messageWindow.dp_Messages.Children.Clear();

            if (amount == 0)
            {
                if (originFormat == ImageFormat.ALL)
                    message.Content = "No supported Image found to convert";
                else
                    message.Content = $"No {originFormat}-Image found to convert";
                messageWindow.dp_Messages.Children.Add(message);
                messageWindow.UpdateTime(DateTime.Now - startTime, "time taken:", true);
                messageWindow.ShowDialog();
            }
            else
            {
                if (originFormat == ImageFormat.ALL)
                {
                    if (bmp != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{bmp} BMP-{(bmp == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }
                    if (emf != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{emf} EMF-{(emf == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }
                    if (exif != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{exif} EXIF-{(exif == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }
                    if (icon != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{icon} ICON-{(icon == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }
                    if (jpg != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{jpg} JPG-{(jpg == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }
                    if (png != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{png} PNG-{(png == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }
                    if (tiff != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{tiff} TIFF-{(tiff == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }
                    if (wmf != 0)
                    {
                        message = CreateNewLabel(message);
                        message.Content = $"{wmf} WMF-{(wmf == 1 ? "Image" : "Images")} converted to {targetFormat}";
                        messageWindow.dp_Messages.Children.Add(message);
                    }

                    messageWindow.Height = messageWindow.Height + 16f * (messageWindow.dp_Messages.Children.Count - 1);
                    messageWindow.dp_Messages.Height = messageWindow.dp_Messages.Height + 16f * (messageWindow.dp_Messages.Children.Count - 1);
                    messageWindow.UpdateTime(DateTime.Now - startTime, "time taken:", true);
                    messageWindow.ShowDialog();
                }
                else
                {
                    switch (originFormat)
                    {
                        case ImageFormat.BMP:
                            message.Content = $"{bmp} {originFormat}-{(bmp == 1 ? "Image" : "Images")} converted to {targetFormat}";
                            break;
                        case ImageFormat.EMF:
                            message.Content = $"{emf} {originFormat}-{(emf == 1 ? "Image" : "Images")} converted to {targetFormat}";
                            break;
                        case ImageFormat.EXIF:
                            message.Content = $"{exif} {originFormat}-{(exif == 1 ? "Image" : "Images")}  converted to {targetFormat}";
                            break;
                        case ImageFormat.ICO:
                            message.Content = $"{icon} {originFormat}-{(icon == 1 ? "Image" : "Images")} converted to {targetFormat}";
                            break;
                        case ImageFormat.JPG:
                            message.Content = $"{jpg} {originFormat}-{(jpg == 1 ? "Image" : "Images")}  converted to {targetFormat}";
                            break;
                        case ImageFormat.PNG:
                            message.Content = $"{png} {originFormat}-{(png == 1 ? "Image" : "Images")}  converted to {targetFormat}";
                            break;
                        case ImageFormat.TIFF:
                            message.Content = $"{tiff} {originFormat}-{(tiff == 1 ? "Image" : "Images")}   converted to {targetFormat}";
                            break;
                        case ImageFormat.WMF:
                            message.Content = $"{wmf} {originFormat}-{(wmf == 1 ? "Image" : "Images")}   converted to {targetFormat}";

                            break;
                    }
                    messageWindow.dp_Messages.Children.Add(message);
                    messageWindow.UpdateTime(DateTime.Now - startTime, "time taken:", true);
                    messageWindow.ShowDialog();
                }
            }
        }

        private void ThreadShowResult()
        {
            while (loadingWindow == null) { }
            CloseLoadingWindow(loadingWindow);
            Dispatcher.Invoke(ShowResult);
        }

        private void Convert(List<string> files)
        {
            string fileending;
            string[] tmp;

            foreach (string file in files)
            {
                // get fileending
                tmp = file.Split('.');
                fileending = tmp[tmp.Length - 1].ToUpper();

                // check format and convert if its the chosen format to convert
                switch (fileending)
                {
                    case "BMP":
                        if (originFormat == ImageFormat.BMP || originFormat == ImageFormat.ALL)
                        {
                            bmp += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "EMF":
                        if (originFormat == ImageFormat.EMF || originFormat == ImageFormat.ALL)
                        {
                            emf += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "EXIF":
                        if (originFormat == ImageFormat.EXIF || originFormat == ImageFormat.ALL)
                        {
                            exif += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "ICON":
                        if (originFormat == ImageFormat.ICO || originFormat == ImageFormat.ALL)
                        {
                            icon += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "JPG":
                        if (originFormat == ImageFormat.JPG || originFormat == ImageFormat.ALL)
                        {
                            jpg += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "JPEG":
                        if (originFormat == ImageFormat.JPG || originFormat == ImageFormat.ALL)
                        {
                            jpg += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "JPG_LARGE":
                        if (originFormat == ImageFormat.JPG || originFormat == ImageFormat.ALL)
                        {
                            jpg += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "PNG":
                        if (originFormat == ImageFormat.PNG || originFormat == ImageFormat.ALL)
                        {
                            png += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "TIFF":
                        if (originFormat == ImageFormat.TIFF || originFormat == ImageFormat.ALL)
                        {
                            tiff += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                    case "WMF":
                        if (originFormat == ImageFormat.WMF || originFormat == ImageFormat.ALL)
                        {
                            wmf += 1;
                            ConvertImageChoser(file);
                        }
                        break;
                }
                // calculate current count of finished converted images
                countAllCurrent = bmp + emf + exif + icon + jpg + png + tiff + wmf;

                if (loadingWindow != null && countAllCurrent > countAllPast)
                {
                    // set new past count of finished converted images
                    countAllPast = countAllCurrent;

                    // update loadingWindow
                    loadingWindow.UpdateStatus(countAllCurrent, amount);
                }
            }
        }

        private void ConvertImageChoser(string file)
        {
            switch (targetFormat)
            {
                case ImageFormat.BMP:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Bmp, delete);
                    break;
                case ImageFormat.EMF:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Emf, delete);
                    break;
                case ImageFormat.EXIF:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Exif, delete);
                    break;
                case ImageFormat.ICO:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Icon, delete);
                    break;
                case ImageFormat.JPG:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Jpeg, delete);
                    break;
                case ImageFormat.PNG:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Png, delete);
                    break;
                case ImageFormat.TIFF:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Tiff, delete);
                    break;
                case ImageFormat.WMF:
                    ConvertImage(file, System.Drawing.Imaging.ImageFormat.Wmf, delete);
                    break;
            }
        }

        private void OpenLoadingWindow()
        {
            loadingWindow = new MessageWindow();

            loadingWindow.l_Titel.Content = "Converting . . .";
            loadingWindow.b_Close.IsEnabled = false;
            loadingWindow.Show();
            loadingWindow.Loop();
            System.Windows.Threading.Dispatcher.Run();
        }

        private void CloseLoadingWindow(Window window)
        {
            if (window.Dispatcher.CheckAccess())
            {
                window.Close();
            }
            else
            {
                window.Dispatcher.Invoke(new ThreadStart(window.Close));
            }
        }

        private Label CreateNewLabel(Label label)
        {
            label = new Label();
            label.Padding = new Thickness(5, 0, 5, 0);
            label.Foreground = Brushes.White;
            DockPanel.SetDock(label, Dock.Top);
            return label;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenInformationWindow(object sender, RoutedEventArgs e)
        {
            MessageWindow messageWindow = new MessageWindow();
            messageWindow.l_Timer.Content = "";
            messageWindow.l_Titel.Content = "Informations about ImageConverter";
            messageWindow.dp_Messages.Children.Clear();

            Label message = new Label();
            message = CreateNewLabel(message);
            message.Content = "Convert all images in selected folder only";
            messageWindow.dp_Messages.Children.Add(message);

            message = CreateNewLabel(message);
            string text = "Supported imageformats:";
            string space = "\t" + "\t" + "\t";

            for (int i = 0; i < (int)ImageFormat.ALL; i++)
            {
                if (i == 0)
                    message.Content = text + "\t" + ((ImageFormat)i).ToString().ToUpper();
                else
                {
                    message = CreateNewLabel(message);
                    message.Content = space + ((ImageFormat)i).ToString().ToUpper();
                }
                messageWindow.dp_Messages.Children.Add(message);
                messageWindow.Height += 16;
                messageWindow.dp_Messages.Height += 16;
            }

            messageWindow.ShowDialog();
        }

        private void cb_ChosenFormatToConvert_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // set originFormat
            originFormat = (ImageFormat)((ComboBox)sender).SelectedIndex;
        }

        private void cb_TargetFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // set targetFormat
            targetFormat = (ImageFormat)((ComboBox)sender).SelectedIndex;
        }

        public enum ImageFormat
        {
            BMP,
            EMF,
            EXIF,
            ICO,
            JPG,
            PNG,
            TIFF,
            WMF,
            ALL
        }

        private void cb_deleteOldFiles_Checked(object sender, RoutedEventArgs e)
        {
            delete = ((CheckBox)sender).IsChecked;
        }

        private void cb_subfolder_Checked(object sender, RoutedEventArgs e)
        {
            subfolder = ((CheckBox)sender).IsChecked;
        }
    }
}
