﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CaffemodelLoader;
using KelpNet.Common;
using KelpNet.Common.Functions;
using KelpNet.Common.Tools;
using KelpNet.Functions;
using KelpNet.Functions.Connections;
using KelpNet.Functions.Poolings;
using TestDataManager;

namespace KelpNetTester.Tests
{
    //CaffeモデルのVGG16を読み込んで画像分類をさせるテスト
    class Test15
    {
        const string DOWNLOAD_URL = "http://www.robots.ox.ac.uk/~vgg/software/very_deep/caffe/";
        const string MODEL_FILE = "VGG_ILSVRC_16_layers.caffemodel";
        private const string CLASS_LIST_PATH = "data/synset_words.txt";

        public static void Run()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "画像ファイル(*.jpg;*.png)|*.jpg;*.png|すべてのファイル(*.*)|*.*" };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Bitmap baseImage = new Bitmap(ofd.FileName);

                string modelFilePath = InternetFileDownloader.Donwload(DOWNLOAD_URL + MODEL_FILE, MODEL_FILE);
                List<Function> vgg16Net = CaffemodelDataLoader.ModelLoad(modelFilePath);
                string[] classList = File.ReadAllLines(CLASS_LIST_PATH);

                //GPUを初期化
                for (int i = 0; i < vgg16Net.Count-1; i++)
                {
                    if (vgg16Net[i] is Convolution2D)
                    {
                        ((Convolution2D)vgg16Net[i]).InitGpu();
                    }
                    else if (vgg16Net[i] is Linear)
                    {
                        ((Linear)vgg16Net[i]).InitGpu();
                    }
                    else if (vgg16Net[i] is MaxPooling)
                    {
                        ((MaxPooling)vgg16Net[i]).InitGpu();
                    }
                }

                FunctionStack nn = new FunctionStack(vgg16Net.ToArray());

                //層を圧縮
                nn.Compress();

                //ネットワークへ入力する前に解像度を 224px x 224px x 3ch にしておく
                Bitmap resultImage = new Bitmap(224, 224, PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(resultImage);
                g.DrawImage(baseImage, 0, 0, 224, 224);
                g.Dispose();

                BatchArray image = new BatchArray(NdArrayConverter.Image2NdArray(resultImage));

                Stopwatch sw = Stopwatch.StartNew();
                BatchArray result = nn.Predict(image);
                sw.Stop();

                Console.WriteLine("Result Time : " + (sw.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L))).ToString("n0") + "μｓ");

                int maxIndex = Array.IndexOf(result.Data, result.Data.Max());
                Console.WriteLine("[" + result.Data[maxIndex] + "] : " + classList[maxIndex]);
            }
        }
    }
}