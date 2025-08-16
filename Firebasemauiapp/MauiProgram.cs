﻿using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebasemauiapp.Pages;
using Firebasemauiapp.Mainpages;
using Firebasemauiapp.Data;
using Firebasemauiapp.Services;
using Microsoft.Extensions.Logging;

namespace Firebasemauiapp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig()
		{
			ApiKey = "AIzaSyCtqanoTU24UXz82KyZI8phmYae09sIx5U",
			AuthDomain = "righthere-backend.firebaseapp.com",
			Providers =
			[
				new EmailProvider()
			],
		}));

		// เพิ่ม Firestore services
		builder.Services.AddSingleton<FirestoreService>();
		builder.Services.AddSingleton<DiaryDatabase>();

		// View Models
		builder.Services.AddTransient<SignInViewModel>();
		builder.Services.AddTransient<SignUpViewModel>();
		builder.Services.AddTransient<StarterViewModel>();
		builder.Services.AddTransient<DiaryViewModel>();
		builder.Services.AddTransient<SummaryViewModel>();
		builder.Services.AddTransient<DiaryHistoryViewModel>();

		// Views
		builder.Services.AddTransient<SignInView>();
		builder.Services.AddTransient<SignUpView>();
		builder.Services.AddTransient<StarterView>();
		builder.Services.AddTransient<DiaryView>();
		builder.Services.AddTransient<SummaryView>();
		builder.Services.AddTransient<DiaryHistory>();


		return builder.Build();
	}
}
