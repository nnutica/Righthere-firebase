using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;
using Firebasemauiapp.CommunityPage;
using Firebasemauiapp.Data;
using Firebasemauiapp.Services;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Syncfusion.Maui.Core.Hosting;
using Microsoft.Maui.Storage;
using System.IO;
using Firebasemauiapp.Config;

namespace Firebasemauiapp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("LeagueSpartan-ExtraBold.ttf", "AppFont");
				fonts.AddFont("LeagueSpartan-Bold.ttf", "contextfont");
				fonts.AddFont("LeagueSpartan-Light.ttf", "lightfont");
				fonts.AddFont("LeagueSpartan-Regular.ttf", "regularfont");
				fonts.AddFont("LeagueSpartan-Medium.ttf", "mediumfont");

			})
			.ConfigureMauiHandlers(handlers =>
			{
#if ANDROID
				Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping("NoUppercase", (handler, view) =>
				{
					handler.PlatformView.SetAllCaps(false);
				});

				Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
				{
					handler.PlatformView.BackgroundTintList = 
						global::Android.Content.Res.ColorStateList.ValueOf(global::Android.Graphics.Color.Transparent);
				});
#endif
			})
			.ConfigureSyncfusionCore();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Configure FirebaseAuthClient with persistent session storage
		var authConfig = new FirebaseAuthConfig()
		{
			ApiKey = "AIzaSyCtqanoTU24UXz82KyZI8phmYae09sIx5U",
			AuthDomain = "righthere-backend.firebaseapp.com",
			Providers =
			[
				new EmailProvider(),
				new GoogleProvider()
			],
			// Persist the user session securely in the app data directory
			UserRepository = new FileUserRepository(FileSystem.AppDataDirectory)
		};

		builder.Services.AddSingleton(new FirebaseAuthClient(authConfig));

		// เพิ่ม Firestore services
		builder.Services.AddSingleton<FirestoreService>();
		builder.Services.AddSingleton<DiaryDatabase>();
		builder.Services.AddSingleton<PostDatabase>();
		builder.Services.AddSingleton<AuthRoutingService>();
		builder.Services.AddSingleton<GitHubUploadService>();

		// View Models
		builder.Services.AddTransient<SignInViewModel>();
		builder.Services.AddTransient<SignUpViewModel>();
		builder.Services.AddTransient<StarterViewModel>();
		builder.Services.AddTransient<DiaryViewModel>();
		builder.Services.AddTransient<MoodViewModel>();
		builder.Services.AddTransient<SummaryViewModel>();
		builder.Services.AddTransient<DiaryHistoryViewModel>();
		builder.Services.AddTransient<HistoryDetailViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<CommunityViewModel>();
		builder.Services.AddTransient<CreatePostViewModel>();
		builder.Services.AddTransient<LevelMoodViewModel>();
		builder.Services.AddTransient<Firebasemauiapp.QuestPage.QuestViewModel>();
		builder.Services.AddTransient<Firebasemauiapp.StorePage.StoreViewModel>();

		// Views
		builder.Services.AddTransient<SignInView>();
		builder.Services.AddTransient<SignUpView>();
		builder.Services.AddTransient<StarterView>();
		builder.Services.AddTransient<DiaryView>();
		builder.Services.AddTransient<SelectMoodPage>();
		builder.Services.AddTransient<SummaryView>();
		builder.Services.AddTransient<SummaryMockView>();
		builder.Services.AddTransient<DiaryHistory>();
		builder.Services.AddTransient<HistoryDetailPage>();
		builder.Services.AddTransient<Dashboard>();
		builder.Services.AddTransient<CommunityPage.CommunityPage>();
		builder.Services.AddTransient<CommunityPage.CommunityCreatPostPage>();
		builder.Services.AddTransient<Firebasemauiapp.QuestPage.QuestPage>();
		builder.Services.AddTransient<Firebasemauiapp.StorePage.StorePage>();
		builder.Services.AddTransient<LevelMoodPage>();

		var app = builder.Build();

		// Expose service provider and start auth-driven routing
		ServiceHelper.Initialize(app.Services);
		// Apply default GitHub upload settings to Preferences (fill values in Config/GitHubSettings.cs)
		GitHubSettings.ApplyToPreferences();
		_ = app.Services.GetRequiredService<FirestoreService>().GetDatabaseAsync();

		return app;
	}
}
