using Microsoft.Maui.Controls;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает функционал приложения
	/// </summary>
	public partial class App: Application
		{
		#region Общие переменные и константы

		// Флаги запуска
		private RDAppStartupFlags flags;

		// Цветовая схема
		private readonly Color
			solutionMasterBackColor = Color.FromArgb ("#F0F8FF"),
			solutionFieldBackColor = Color.FromArgb ("#D0E8FF"),

			linksMasterBackColor = Color.FromArgb ("#FFFFF0"),
			linksLinkBackColor = Color.FromArgb ("#FFFFE0"),
			linksFieldBackColor = Color.FromArgb ("#FFFFD0"),
			linkColor = Color.FromArgb ("#6000C0"),

			aboutMasterBackColor = Color.FromArgb ("#F0FFF0"),
			aboutFieldBackColor = Color.FromArgb ("#D0FFD0");

		// Состояние окна просмотра
		private int currentTagNumber = 0;
		private int currentLinksSegment = 0;
		private int segmentsCount;

		// Загруженные страницы и данные
		private List<string> linksURLs = new List<string> ();
		private List<View> views = new List<View> ();

		// Состояния методов загрузки
		private bool linksResetInProgress = false;

		// Число фиксированных ссылок
		private const uint lockedURLCount = 4;

		#endregion

		#region Переменные страниц

		private ContentPage solutionPage, aboutPage, linksPage;

		private Label aboutLabel, linksFontSizeFieldLabel, aboutFontSizeField,
			postsPerPageFieldLabel;

		private Button allNewsButton, currentPageButton, saveButton,
			segmentDecButton, segmentIncButton, segmentLabel,
			fontSizeIncButton, fontSizeDecButton, postsPerPageIncButton, postsPerPageDecButton;

		private StackLayout mainLinksLayout;
		private ScrollView mainScroll;

		private List<string> pageVariants = new List<string> ();

		#endregion

		#region Запуск и настройка

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
			{
			// Инициализация
			InitializeComponent ();
			flags = AndroidSupport.GetAppStartupFlags (RDAppStartupFlags.DisableXPUN |
				RDAppStartupFlags.CanReadFiles | RDAppStartupFlags.CanWriteFiles);

			if (ProgramDescription.NSet == null)
				ProgramDescription.NSet = new TPNotification ();
			if (!RDLocale.IsCurrentLanguageRuRu)
				RDLocale.CurrentLanguage = RDLanguages.ru_ru;

			// Переход в статус запуска для отмены вызова из оповещения
			AndroidSupport.AppIsRunning = true;

			// Общая конструкция страниц приложения
			MainPage = new MasterPage ();

			solutionPage = AndroidSupport.ApplyPageSettings (new SolutionPage (), "SolutionPage",
				"Настройки", solutionMasterBackColor);
			aboutPage = AndroidSupport.ApplyPageSettings (new AboutPage (), "AboutPage",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
				aboutMasterBackColor);
			linksPage = AndroidSupport.ApplyPageSettings (new LinksPage (), "LinksPage",
				"Журнал", linksMasterBackColor);

			AndroidSupport.SetMasterPage (MainPage, linksPage, linksMasterBackColor);

			#region Настройки службы и управление ссылками

			allNewsButton = AndroidSupport.ApplyButtonSettings (linksPage, "AllNewsButton",
				RDDefaultButtons.Refresh, linksFieldBackColor, AllNewsItems);
			saveButton = AndroidSupport.ApplyButtonSettings (linksPage, "SaveButton",
				RDDefaultButtons.Down, linksFieldBackColor, SaveCurrentPageToFile);
			currentPageButton = AndroidSupport.ApplyButtonSettings (linksPage, "CurrentPageButton",
				"", linksFieldBackColor, SelectCurrentPage, true);
			currentPageButton.Margin = new Thickness (0);

			currentTagNumber = (int)NotificationsSupport.CurrentTagNumber;

			if (ProgramDescription.NSet.Tags.Count > currentTagNumber)
				{
				currentPageButton.Text = ProgramDescription.NSet.Tags[currentTagNumber];
				}
			else
				{
				currentPageButton.IsVisible = saveButton.IsVisible = false;
				currentTagNumber = 0;
				}

			// Движение по сегментам
			segmentLabel = AndroidSupport.ApplyButtonSettings (linksPage, "SegmentLabel", "1", linksFieldBackColor,
				SelectSegment, false);
			segmentLabel.Margin = segmentLabel.Padding = Thickness.Zero;

			segmentDecButton = AndroidSupport.ApplyButtonSettings (linksPage, "SegmentDecButton",
				RDDefaultButtons.Backward, linksFieldBackColor, SegmentChanged);
			segmentIncButton = AndroidSupport.ApplyButtonSettings (linksPage, "SegmentIncButton",
				RDDefaultButtons.Start, linksFieldBackColor, SegmentChanged);
			segmentDecButton.WidthRequest = segmentIncButton.WidthRequest = 1.5 * AndroidSupport.MasterFontSize;

			// Меню
			AndroidSupport.ApplyButtonSettings (linksPage, "MenuButton", RDDefaultButtons.Menu,
				linksFieldBackColor, SelectPage);

			// Связанные настройки
			AndroidSupport.ApplyLabelSettings (solutionPage, "GenericLabel", "Общие настройки",
				RDLabelTypes.HeaderLeft);

			linksFontSizeFieldLabel = AndroidSupport.ApplyLabelSettings (solutionPage,
				"LinksFontSizeFieldLabel", "", RDLabelTypes.DefaultLeft);
			fontSizeIncButton = AndroidSupport.ApplyButtonSettings (solutionPage, "LinksFontSizeIncButton",
				RDDefaultButtons.Increase, solutionFieldBackColor, LinksFontSizeChanged);
			fontSizeDecButton = AndroidSupport.ApplyButtonSettings (solutionPage, "LinksFontSizeDecButton",
				RDDefaultButtons.Decrease, solutionFieldBackColor, LinksFontSizeChanged);
			LinksFontSizeChanged (null, null);

			postsPerPageFieldLabel = AndroidSupport.ApplyLabelSettings (solutionPage,
				"PostsPerPageFieldLabel", "", RDLabelTypes.DefaultLeft);
			postsPerPageIncButton = AndroidSupport.ApplyButtonSettings (solutionPage, "PostsPerPageIncButton",
				RDDefaultButtons.Increase, solutionFieldBackColor, PostsPerPagesChanged);
			postsPerPageDecButton = AndroidSupport.ApplyButtonSettings (solutionPage, "PostsPerPageDecButton",
				RDDefaultButtons.Decrease, solutionFieldBackColor, PostsPerPagesChanged);
			PostsPerPagesChanged (null, null);

			// Настройка стационарных ссылок
			AndroidSupport.ApplyLabelSettings (solutionPage, "LockedLinksLabel", "Стационарные ссылки",
				RDLabelTypes.HeaderLeft);
			AndroidSupport.ApplyButtonSettings (solutionPage, "LockedLinksSetButton",
				"Настроить ссылку", solutionFieldBackColor, SetLockedLink_Clicked, false);
			AndroidSupport.ApplyButtonSettings (solutionPage, "LockedLinksDeleteButton",
				"Сбросить ссылку", solutionFieldBackColor, ResetLockedLink_Clicked, false);

			AndroidSupport.ApplyButtonSettings (solutionPage, "LoadLinksButton",
				"Загрузить ссылки", solutionFieldBackColor, LoadLinksButton_Clicked, false);
			AndroidSupport.ApplyButtonSettings (solutionPage, "SaveLinksButton",
				"Сохранить ссылки", solutionFieldBackColor, SaveLinksButton_Clicked, false);

			Label allowServiceTip;
			Button allowServiceButton;

			// Не работают файловые операции
			if (!flags.HasFlag (RDAppStartupFlags.CanReadFiles) ||
				!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				{
				allowServiceTip = AndroidSupport.ApplyLabelSettings (solutionPage, "LoadSaveTip",
					RDLocale.GetDefaultText (RDLDefaultTexts.Message_ReadWritePermission), RDLabelTypes.ErrorTip);

				allowServiceButton = AndroidSupport.ApplyButtonSettings (solutionPage, "LoadSaveButton",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo),
					solutionFieldBackColor, CallAppSettings, false);
				allowServiceButton.HorizontalOptions = LayoutOptions.Center;
				}

			// Нормальный запуск
			else
				{
				allowServiceTip = AndroidSupport.ApplyLabelSettings (solutionPage, "LoadSaveTip",
					" ", RDLabelTypes.TipCenter);
				allowServiceTip.IsVisible = false;

				allowServiceButton = AndroidSupport.ApplyButtonSettings (solutionPage, "LoadSaveButton",
					" ", solutionFieldBackColor, null, false);
				allowServiceButton.IsVisible = false;
				}

			for (int i = 0; i < lockedURLCount; i++)
				AndroidSupport.ApplyButtonSettings (linksPage, "LockedLinkButton" + (i + 1).ToString (),
					RDDefaultButtons.SpecialOne + i, linksFieldBackColor, LockedLinks_Clicked);

			#endregion

			#region Страница "О программе"

			aboutLabel = AndroidSupport.ApplyLabelSettings (aboutPage, "AboutLabel",
				ProgramDescription.AssemblyTitle + "\n" +
				ProgramDescription.AssemblyDescription + "\n\n" +
				RDGenerics.AssemblyCopyright + ", Соля́ников Я.\nv " +
				ProgramDescription.AssemblyVersion +
				"; " + ProgramDescription.AssemblyLastUpdate,
				RDLabelTypes.AppAbout);

			AndroidSupport.ApplyButtonSettings (aboutPage, "ManualsButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_ReferenceMaterials),
				aboutFieldBackColor, ReferenceButton_Click, false);
			AndroidSupport.ApplyButtonSettings (aboutPage, "HelpButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click, false);
			AndroidSupport.ApplyLabelSettings (aboutPage, "GenericSettingsLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_GenericSettings),
				RDLabelTypes.HeaderLeft);

			AndroidSupport.ApplyLabelSettings (aboutPage, "RestartTipLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Message_RestartRequired),
				RDLabelTypes.TipCenter);

			AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceFontSize),
				RDLabelTypes.DefaultLeft);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeInc",
				RDDefaultButtons.Increase, aboutFieldBackColor, FontSizeButton_Clicked);
			AndroidSupport.ApplyButtonSettings (aboutPage, "FontSizeDec",
				RDDefaultButtons.Decrease, aboutFieldBackColor, FontSizeButton_Clicked);
			aboutFontSizeField = AndroidSupport.ApplyLabelSettings (aboutPage, "FontSizeField",
				" ", RDLabelTypes.DefaultCenter);

			FontSizeButton_Clicked (null, null);

			#endregion

			// Сброс поля ссылок
			mainLinksLayout = (StackLayout)linksPage.FindByName ("MainLayout");
			mainScroll = (ScrollView)linksPage.FindByName ("MainScroll");
			ResetLinksField ();

			// Принятие соглашений
			ShowStartupTips ();
			}

		// Методы отображают подсказки по работе с приложением
		private async void ShowStartupTips ()
			{
			// Контроль XPUN
			if (!flags.HasFlag (RDAppStartupFlags.DisableXPUN))
				await AndroidSupport.XPUNLoop ();

			// Требование принятия Политики
			if (!TipsState.HasFlag (NSTipTypes.PolicyTip))
				{
				await AndroidSupport.PolicyLoop ();
				TipsState |= NSTipTypes.PolicyTip;
				}

			// Подсказки
			if (!TipsState.HasFlag (NSTipTypes.StartupTips))
				{
				await AndroidSupport.ShowMessage
					("Добро пожаловать на главную страницу приложения Tablepedia notifier!" + RDLocale.RNRN +
					"Это облако ссылок заполнится автоматически при первом обновлении состояния",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next));

				await AndroidSupport.ShowMessage
					("Внимание! Некоторые устройства требуют ручного разрешения на доступ в интернет " +
					"(например, если активен режим экономии интернет-трафика). Проверьте его, если " +
					"приложение не будет работать",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

				TipsState |= NSTipTypes.StartupTips;
				}
			}

		/// <summary>
		/// Сохранение настроек программы
		/// </summary>
		protected override void OnSleep ()
			{
			// Сохранение настроек
			NotificationsSupport.CurrentTagNumber = (uint)currentTagNumber;
			ProgramDescription.NSet.SaveSettings ();
			}

		// Вызов настроек приложения (для Android 12 и выше)
		private void CallAppSettings (object sender, EventArgs e)
			{
			AndroidSupport.CallAppSettings ();
			}

		/// <summary>
		/// Возвращает или задаёт состав флагов просмотра справочных сведений
		/// </summary>
		public static NSTipTypes TipsState
			{
			get
				{
				return (NSTipTypes)RDGenerics.GetSettings (tipsStatePar, 0);
				}
			set
				{
				RDGenerics.SetSettings (tipsStatePar, (uint)value);
				}
			}
		private const string tipsStatePar = "TipsState";

		/// <summary>
		/// Доступные типы уведомлений
		/// </summary>
		public enum NSTipTypes
			{
			/// <summary>
			/// Интерфейс принятия Политики
			/// </summary>
			PolicyTip = 0x0001,

			/// <summary>
			/// Начальные подсказки
			/// </summary>
			StartupTips = 0x0002,
			}

		#endregion

		#region Страница ссылок

		// Сброс и перезагрузка состояния поля ссылок
		private async Task<bool> ResetLinksField ()
			{
			// Блокировка
			segmentDecButton.IsEnabled = segmentIncButton.IsEnabled = segmentLabel.IsEnabled =
				fontSizeIncButton.IsEnabled = fontSizeDecButton.IsEnabled =
				postsPerPageIncButton.IsEnabled = postsPerPageDecButton.IsEnabled =
				currentPageButton.IsEnabled = saveButton.IsEnabled = allNewsButton.IsEnabled = false;
			currentPageButton.Text = "Выполняется загрузка...";

			// Сброс состояния
			linksURLs.Clear ();
			mainLinksLayout.Children.Clear ();
			views.Clear ();

			if (ProgramDescription.NSet.Tags.Count > 0)
				segmentsCount = (ProgramDescription.NSet.TagsPostsLinks[currentTagNumber].Count - 1) /
					(int)NotificationsSupport.PostsPerPage + 1;
			else
				segmentsCount = 0;

			// Защита
			segmentIncButton.IsVisible = segmentDecButton.IsVisible = segmentLabel.IsVisible = (segmentsCount > 1);
			if (segmentsCount < 1)
				goto fail;

			// Обновление сегмента
			if (currentLinksSegment > segmentsCount - 1)
				currentLinksSegment = 0;
			SegmentChanged (null, null);

			// Разбор конструкции
			if (ProgramDescription.NSet.Tags.Count > 0)
				{
				Thread.Sleep (100);
				if (!await Task.Run<bool> (ResetLinksExecutor))
					goto fail;
				}

			// Загрузка контролов в представление
			for (int i = 0; i < views.Count; i++)
				mainLinksLayout.Children.Add (views[i]);

			mainScroll.HeightRequest = linksPage.Height - (currentPageButton.Height + segmentLabel.Height);

			// Успешно
			segmentDecButton.IsEnabled = (currentLinksSegment > 0);
			segmentIncButton.IsEnabled = (currentLinksSegment < segmentsCount - 1);
			segmentLabel.IsEnabled = segmentLabel.IsVisible;

			currentPageButton.IsEnabled = saveButton.IsEnabled = allNewsButton.IsEnabled =
				fontSizeIncButton.IsEnabled = fontSizeDecButton.IsEnabled =
				postsPerPageIncButton.IsEnabled = postsPerPageDecButton.IsEnabled = true;
			currentPageButton.Text = ProgramDescription.NSet.Tags[currentTagNumber];

			return true;

			// С ошибкой
			fail:
			allNewsButton.IsEnabled = fontSizeIncButton.IsEnabled = fontSizeDecButton.IsEnabled =
				postsPerPageIncButton.IsEnabled = postsPerPageDecButton.IsEnabled = true;
			currentPageButton.Text = " ";

			return false;
			}

		private async Task<bool> ResetLinksExecutor ()
			{
			// Защита
			if (linksResetInProgress)
				return false;
			linksResetInProgress = true;

			// Инициализация
			int t = currentTagNumber, s = currentLinksSegment;   // Отрыв от глобального состояния
			int[] indexes = ProgramDescription.NSet.TagsPostsLinks[t].ToArray ();

			for (int i = s * (int)NotificationsSupport.PostsPerPage;
				(i < (s + 1) * (int)NotificationsSupport.PostsPerPage) && (i < indexes.Length); i++)
				{
				// Извлечение сегментов
				string header = ProgramDescription.NSet.PostHeaders[indexes[i]];
				string tags = ProgramDescription.NSet.PostTags[indexes[i]];
				string link = ProgramDescription.NSet.PostLinks[indexes[i]];
				string text = ProgramDescription.NSet.PostTexts[indexes[i]];

				string image;
				if (flags.HasFlag (RDAppStartupFlags.CanReadFiles) &&
					flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
					image = await ProgramDescription.NSet.GetPostPicture ((uint)indexes[i]);
				else
					image = "";

				string imageTip = ProgramDescription.NSet.PostImagesNames[indexes[i]];
				string textFileLink = ProgramDescription.NSet.TextFilesLinks[indexes[i]];

				// Заголовок
				Label lh = new Label ();

				lh.BackgroundColor = linksFieldBackColor;
				lh.FontAttributes = FontAttributes.Bold;
				lh.FontSize = 9 * NotificationsSupport.LinksFontSize / 8;
				lh.HorizontalTextAlignment = TextAlignment.Center;
				lh.Margin = new Thickness (3, 3, 3, 3);
				lh.HorizontalOptions = LayoutOptions.Fill;
				lh.Padding = new Thickness (0);
				lh.Text = header;
				lh.TextColor = linksFontSizeFieldLabel.TextColor;
				lh.TextTransform = TextTransform.None;
				lh.TextType = TextType.Text;

				linksURLs.Add ("");
				views.Add (lh);

				// Теги
				Button lt = new Button ();

				lt.BackgroundColor = linksFieldBackColor;
				lt.FontAttributes = FontAttributes.Italic;
				lt.FontSize = 7 * NotificationsSupport.LinksFontSize / 8;
				lt.Margin = new Thickness (3, 3, 3, 3);
				lt.HorizontalOptions = LayoutOptions.Fill;
				lt.Padding = new Thickness (0);
				lt.Text = tags;
				lt.TextTransform = TextTransform.None;
				lt.LineBreakMode = LineBreakMode.WordWrap;

				if (string.IsNullOrWhiteSpace (link))
					{
					lt.TextColor = linksFontSizeFieldLabel.TextColor;
					}
				else
					{
					lt.TextColor = linkColor;
					lt.Clicked += LinkStart;
					}

				linksURLs.Add (link);
				views.Add (lt);

				if (!string.IsNullOrWhiteSpace (image))
					{
					Image img = new Image ();
					bool noImage = false;
					try
						{
						img.Source = ImageSource.FromFile (image);
						}
					catch
						{
						noImage = true;
						}

					if (!noImage)
						{
						img.HeightRequest = AndroidSupport.MasterFontSize * 15.0;
						img.HorizontalOptions = LayoutOptions.Fill;
						img.Aspect = Aspect.AspectFit;
						img.Margin = new Thickness (18, 3, 18, 0);

						linksURLs.Add ("");
						views.Add (img);
						}

					Label li = new Label ();

					li.BackgroundColor = linksFieldBackColor;
					li.FontAttributes = FontAttributes.Italic;
					li.FontSize = 7 * NotificationsSupport.LinksFontSize / 8;
					li.HorizontalTextAlignment = TextAlignment.Center;
					li.Margin = new Thickness (3, 3, 3, 3);
					li.HorizontalOptions = LayoutOptions.Center;
					li.Padding = new Thickness (0);

					li.Text = imageTip;
					li.TextColor = linksFontSizeFieldLabel.TextColor;
					li.TextTransform = TextTransform.None;
					li.TextType = TextType.Text;

					if (!noImage && !string.IsNullOrWhiteSpace (imageTip))
						{
						linksURLs.Add ("");
						views.Add (li);
						}
					}

				// Обычный текст
				Label l = new Label ();

				l.HorizontalTextAlignment = TextAlignment.Start;
				l.FontSize = NotificationsSupport.LinksFontSize;
				l.Margin = new Thickness (6, 3, 6, 0);
				l.HorizontalOptions = LayoutOptions.Fill;
				l.Padding = new Thickness (0);
				l.Text = text;
				l.TextColor = linksFontSizeFieldLabel.TextColor;
				l.TextTransform = TextTransform.None;
				l.TextType = TextType.Text;

				linksURLs.Add ("");
				views.Add (l);

				// Текстовый файл
				if (!string.IsNullOrWhiteSpace (textFileLink))
					{
					Button bf = new Button ();

					bf.BackgroundColor = linksLinkBackColor;
					bf.Clicked += LinkStart;
					bf.FontAttributes = FontAttributes.None;
					bf.FontSize = NotificationsSupport.LinksFontSize;
					bf.HorizontalOptions = LayoutOptions.Center;
					bf.Margin = new Thickness (6, 0, 6, 48);
					bf.TextColor = linkColor;
					bf.Text = "Открыть текстовый файл";
					bf.TextTransform = TextTransform.None;

					linksURLs.Add (textFileLink);
					views.Add (bf);
					}
				}

			linksResetInProgress = false;
			return true;
			}

		private void ResetLinksWarning ()
			{
			if (!flags.HasFlag (RDAppStartupFlags.CanReadFiles) ||
				!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				AndroidSupport.ShowBalloon ("Изображения не были загружены, т. к. у приложения нет прав на чтение и " +
					"запись файлов", true);
			}

		// Изменение номера подстраницы
		private async void SelectSegment (object sender, EventArgs e)
			{
			// Запрос
			List<string> pages = new List<string> ();
			for (int i = 0; i < segmentsCount; i++)
				pages.Add ((i + 1).ToString ());

			int res = await AndroidSupport.ShowList ("Выберите страницу",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pages);

			if (res < 0)
				return;

			// Выбор
			currentLinksSegment = res;
			pages.Clear ();

			SegmentChanged (sender, e);
			}

		private async void SegmentChanged (object sender, EventArgs e)
			{
			int oldSegment = currentLinksSegment;

			// Изменение
			if (e != null)
				{
				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Start) &&
					(currentLinksSegment < segmentsCount - 1))
					{
					currentLinksSegment++;
					}
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Backward) &&
					(currentLinksSegment > 0))
					{
					currentLinksSegment--;
					}
				else if (b.Text == segmentLabel.Text)
					{
					oldSegment = -1;
					}
				}

			segmentLabel.Text = (currentLinksSegment + 1).ToString () + "/" + segmentsCount.ToString ();

			if ((e != null) && (oldSegment != currentLinksSegment))
				{
				await ResetLinksField ();
				ResetLinksWarning ();

				if (oldSegment < currentLinksSegment)
					await mainScroll.ScrollToAsync (0, 0, true);
				else
					await mainScroll.ScrollToAsync (0, mainLinksLayout.Height, true);
				}
			}

		// Запуск ссылок
		private async void LinkStart (object sender, EventArgs e)
			{
			try
				{
				int i = 0;
				if ((i = mainLinksLayout.Children.IndexOf ((View)sender)) >= 0)
					{
					if (!string.IsNullOrWhiteSpace (linksURLs[i]))
						await Launcher.OpenAsync (linksURLs[i]);
					}
				}
			catch
				{
				AndroidSupport.ShowBalloon
					(RDLocale.GetDefaultText (RDLDefaultTexts.Message_BrowserNotAvailable), true);
				}
			}

		// Запуск фиксированных ссылок
		private async void LockedLinks_Clicked (object sender, EventArgs e)
			{
			// Выбор ссылки
			Button b = (Button)sender;
			int i;
			for (i = 0; i < lockedURLCount; i++)
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.SpecialOne + i))
					break;

			// Выбор из расширенного списка
			if (i == lockedURLCount - 1)
				{
				// Сборка списка
				List<string> names = ProgramDescription.NSet.LockedLinksNames;
				List<int> idx = new List<int> ();

				for (int j = names.Count - 1; j >= 0; j--)
					{
					if (!ProgramDescription.NSet.IsLinkEmpty ((uint)j))
						{
						names[j] = (j + 1).ToString () + ". " + names[j];
						idx.Add (j);
						}
					else
						{
						names.RemoveAt (j);
						}
					}

				if (names.Count < 1)
					{
					AndroidSupport.ShowBalloon ("Нет настроенных ссылок", true);
					return;
					}

				// Запрос варианта
				int res = await AndroidSupport.ShowList ("Выберите ссылку",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), names);
				if (res < 0)
					{
					names.Clear ();
					return;
					}

				idx.Reverse ();
				i = idx[res];
				names.Clear ();
				}

			// Проверка (хотя такого и не должно быть)
			if (ProgramDescription.NSet.IsLinkEmpty ((uint)i))
				{
				AndroidSupport.ShowBalloon ("Выбранная ссылка не настроена", true);
				return;
				}

			// Запуск
			try
				{
				await Launcher.OpenAsync (ProgramDescription.NSet.LockedLinks[i]);
				}
			catch
				{
				AndroidSupport.ShowBalloon
					(RDLocale.GetDefaultText (RDLDefaultTexts.Message_BrowserNotAvailable), true);
				}
			}

		// Обновление всех страниц
		private async Task<bool> GetAllNot ()
			{
			if (!await ProgramDescription.NSet.Update ())
				return false;

			ProgramDescription.NSet.SaveSettings ();
			return true;
			}

		private async void AllNewsItems (object sender, EventArgs e)
			{
			// Блокировка и сброс
			allNewsButton.IsEnabled = currentPageButton.IsVisible = saveButton.IsVisible =
				segmentIncButton.IsVisible = segmentDecButton.IsVisible = segmentLabel.IsVisible = false;
			mainLinksLayout.Children.Clear ();

			AndroidSupport.ShowBalloon ("Обновление состояния...", true);

			// Запрос
			AndroidSupport.StopRequested = false; // Разблокировка метода GetHTML

			// Опрос
			if (AndroidSupport.AppIsRunning)
				currentPageButton.IsVisible = saveButton.IsVisible = await Task.Run<bool> (GetAllNot);

			// Разблокировка
			allNewsButton.IsEnabled = true;
			if (ProgramDescription.NSet.Tags.Count > 0)
				AndroidSupport.ShowBalloon ("Состояние успешно обновлено", true);

			// Разделы не загружены или их список пуст
			if (!currentPageButton.IsVisible)
				return;

			if (ProgramDescription.NSet.Tags.Count <= currentTagNumber)
				currentTagNumber = 0;

			try
				{
				currentPageButton.Text = ProgramDescription.NSet.Tags[currentTagNumber];
				}
			catch { }

			// Обновления поля ссылок
			await ResetLinksField ();
			ResetLinksWarning ();
			}

		// Выбор текущего раздела
		private async void SelectCurrentPage (object sender, EventArgs e)
			{
			List<string> names = ProgramDescription.NSet.Tags;
			int res = await AndroidSupport.ShowList ("Выберите тег",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), names);

			if (res < 0)
				return;

			currentTagNumber = res;
			currentPageButton.Text = names[currentTagNumber];

			await ResetLinksField ();
			ResetLinksWarning ();
			}

		// Сохранение выгрузки раздела
		private async void SaveCurrentPageToFile (object sender, EventArgs e)
			{
			// Защита
			if (!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				{
				await AndroidSupport.ShowMessage (RDLocale.GetDefaultText
					(RDLDefaultTexts.Message_ReadWritePermission),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				return;
				}

			// Сборка страницы
			int t = currentTagNumber, s = currentLinksSegment;   // Отрыв от глобального состояния
			int[] indexes = ProgramDescription.NSet.TagsPostsLinks[t].ToArray ();

			string html = "<html>\n<head>\n<title>";
			string name = ProgramDescription.NSet.Tags[currentTagNumber];
			html += name + "</title>\n<meta charset=\"utf-8\">\n</head>\n\n<body>\n";

			for (int i = s * (int)NotificationsSupport.PostsPerPage;
				(i < (s + 1) * (int)NotificationsSupport.PostsPerPage) && (i < indexes.Length); i++)
				{
				string header = ProgramDescription.NSet.PostHeaders[indexes[i]];
				string tags = ProgramDescription.NSet.PostTags[indexes[i]];
				string link = ProgramDescription.NSet.PostLinks[indexes[i]];
				string text = ProgramDescription.NSet.PostTexts[indexes[i]];

				html += "<h2>" + header + "</h2>\n";
				html += "<h3><i>" + tags.Replace (RDLocale.RN, "<br>") + "</i></h3>\n";
				html += "<p><a href=\"" + link + "\">Перейти на страницу</a></p><br>\n\n";
				html += "<p>" + text.Replace (RDLocale.RN, "</p><p>") + "</p>\n<br>\n<br>\n\n\n";
				}

			html += "</body>\n";

			// Сохранение
			await AndroidSupport.SaveToFile (name + ".html", html, RDEncodings.UTF8);
			}

		// Выбор текущей страницы
		private async void SelectPage (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pageVariants.Count < 1)
				{
				pageVariants = new List<string> ()
					{
					"Настройки",
					RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
					};
				}

			int res = await AndroidSupport.ShowList (RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pageVariants);
			if (res < 0)
				return;

			// Вызов
			switch (res)
				{
				case 0:
					AndroidSupport.SetCurrentPage (solutionPage, solutionMasterBackColor);
					break;

				case 1:
					AndroidSupport.SetCurrentPage (aboutPage, aboutMasterBackColor);
					break;
				}
			}

		#endregion

		#region Страница О приложении

		// Вызов справочных материалов
		private async void ReferenceButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (RDHelpMaterials.ReferenceMaterials);
			}

		private async void HelpButton_Click (object sender, EventArgs e)
			{
			await AndroidSupport.CallHelpMaterials (RDHelpMaterials.HelpAndSupport);
			}

		// Изменение размера шрифта интерфейса
		private void FontSizeButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					AndroidSupport.MasterFontSize += 0.5;
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					AndroidSupport.MasterFontSize -= 0.5;
				}

			aboutFontSizeField.Text = AndroidSupport.MasterFontSize.ToString ("F1");
			aboutFontSizeField.FontSize = AndroidSupport.MasterFontSize;
			}

		#endregion

		#region Страница настроек

		// Изменение размера шрифта журнала
		private async void LinksFontSizeChanged (object sender, EventArgs e)
			{
			uint oldSize = NotificationsSupport.LinksFontSize;

			if (e != null)
				{
				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase) &&
					(NotificationsSupport.LinksFontSize < AndroidSupport.MaxFontSize))
					{
					NotificationsSupport.LinksFontSize += 1;
					}
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease) &&
					(NotificationsSupport.LinksFontSize > AndroidSupport.MinFontSize))
					{
					NotificationsSupport.LinksFontSize -= 1;
					}
				}

			linksFontSizeFieldLabel.Text = "Размер шрифта журнала: " + NotificationsSupport.LinksFontSize.ToString ();

			if (oldSize != NotificationsSupport.LinksFontSize)
				await ResetLinksField ();
			}

		// Изменение количества постов на странице
		private async void PostsPerPagesChanged (object sender, EventArgs e)
			{
			uint oldValue = NotificationsSupport.PostsPerPage;

			if (e != null)
				{
				Button b = (Button)sender;
				if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Increase) &&
					(NotificationsSupport.PostsPerPage < 10))
					{
					NotificationsSupport.PostsPerPage += 1;
					}
				else if (AndroidSupport.IsNameDefault (b.Text, RDDefaultButtons.Decrease) &&
					(NotificationsSupport.PostsPerPage > 1))
					{
					NotificationsSupport.PostsPerPage -= 1;
					}
				}

			postsPerPageFieldLabel.Text = "Число постов на странице: " + NotificationsSupport.PostsPerPage.ToString ();

			if (oldValue != NotificationsSupport.PostsPerPage)
				await ResetLinksField ();
			}

		// Настройка фиксированных ссылок
		private async void SetLockedLink_Clicked (object sender, EventArgs e)
			{
			await SetLockedLink (false);
			}

		private async void ResetLockedLink_Clicked (object sender, EventArgs e)
			{
			await SetLockedLink (true);
			}

		private async Task<bool> SetLockedLink (bool Reset)
			{
			// Сборка списка
			List<string> names = ProgramDescription.NSet.LockedLinksNames;

			for (int i = 0; i < names.Count; i++)
				{
				if (ProgramDescription.NSet.IsLinkEmpty ((uint)i))
					names[i] = (i + 1).ToString () + ". (пусто)";
				else
					names[i] = (i + 1).ToString () + ". " + names[i];
				}

			// Запрос варианта
			int res = await AndroidSupport.ShowList ("Выберите ссылку",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), names);
			names.Clear ();

			if (res < 0)
				return false;

			if (res < TPNotification.LockedOverridenLinksCount)
				{
				AndroidSupport.ShowBalloon ("Эта ссылка недоступна для изменения", true);
				return false;
				}

			uint position = (uint)res;

			// Сброс (если задан)
			if (Reset)
				{
				AndroidSupport.ShowBalloon ("Ссылка №" + (position + 1).ToString () + " сброшена", true);
				return ProgramDescription.NSet.SetLockedLink (position);
				}

			// Запрос имени
			string name = await AndroidSupport.ShowInput ("Название ссылки", null,
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				50, Keyboard.Text, ProgramDescription.NSet.LockedLinksNames[(int)position]);

			if (string.IsNullOrWhiteSpace (name))
				{
				AndroidSupport.ShowBalloon ("Название не было изменено", true);
				return false;
				}

			// Условие не выполняется только в двух случаях:
			// - когда добавляется новое оповещение, не имеющее аналогов в списке;
			// - когда обновляется текущее выбранное оповещение.
			// Остальные случаи следует считать попыткой задвоения имени
			int idx = ProgramDescription.NSet.LockedLinksNames.IndexOf (name);
			if ((idx >= 0) && (idx != position))
				{
				AndroidSupport.ShowBalloon ("Это название уже присутствует в списке ссылок", true);
				return false;
				}

			// Запрос ссылки
			string oldLink = ProgramDescription.NSet.LockedLinks[(int)position];
			if (ProgramDescription.NSet.IsLinkEmpty (position))
				oldLink = "https://";

			string link = await AndroidSupport.ShowInput ("URL ссылки", null,
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Save),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				150, Keyboard.Url, oldLink);

			if (ProgramDescription.NSet.SetLockedLink (position, name, link) < 0)
				{
				AndroidSupport.ShowBalloon ("Ссылка не была обновлена: URL задан некорректно", true);
				return false;
				}

			// Успешно
			AndroidSupport.ShowBalloon ("Ссылка №" + (position + 1).ToString () + " обновлена", true);
			return true;
			}

		// Загрузка фиксированных ссылок
		private async void LoadLinksButton_Clicked (object sender, EventArgs e)
			{
			// Защита
			if (!flags.HasFlag (RDAppStartupFlags.CanReadFiles))
				{
				await AndroidSupport.ShowMessage (RDLocale.GetDefaultText
					(RDLDefaultTexts.Message_ReadWritePermission),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				return;
				}

			if (!await AndroidSupport.ShowMessage
				("ВНИМАНИЕ: эта опция перезапишет все существующие фиксированные ссылки. Продолжить?",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
				return;

			// Загрузка
			string file = await AndroidSupport.LoadFromFile (RDEncodings.Unicode16);
			if (string.IsNullOrWhiteSpace (file))
				return;

			if (!ProgramDescription.NSet.ReadSettingsFromFile (file))
				{
				AndroidSupport.ShowBalloon
					("Не удалось загрузить ссылки: файл повреждён или не поддерживается", true);
				return;
				}

			// Сброс состояния
			AndroidSupport.ShowBalloon ("Загрузка ссылок успешно завершена", true);
			}

		// Выгрузка фиксированных ссылок
		private async void SaveLinksButton_Clicked (object sender, EventArgs e)
			{
			// Защита
			if (!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
				{
				await AndroidSupport.ShowMessage (RDLocale.GetDefaultText
					(RDLDefaultTexts.Message_ReadWritePermission),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				return;
				}

			// Сохранение
			await AndroidSupport.SaveToFile (TPNotification.SettingsFileName,
				ProgramDescription.NSet.SaveSettingsToFile (), RDEncodings.Unicode16);
			}

		#endregion
		}
	}
