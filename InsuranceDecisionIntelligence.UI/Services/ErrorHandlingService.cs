using InsuranceDecisionIntelligence.UI.Common;
using System;
using System.Collections.Generic;
using System.Windows;

namespace InsuranceDecisionIntelligence.UI.Services
{
    public class ErrorHandlingService
    {
        private static readonly Dictionary<string, string> ErrorMessages = new()
        {
            { "NETWORK_ERROR", "Network connection failed. Please check your internet connection and try again." },
            { "API_ERROR", "Server communication failed. Please try again later." },
            { "VALIDATION_ERROR", "Invalid input provided. Please check your input and try again." },
            { "NOT_FOUND", "The requested resource was not found." },
            { "UNAUTHORIZED", "Access denied. Please check your credentials." },
            { "INTERNAL_ERROR", "An unexpected error occurred. Please try again." },
            { "FILE_ERROR", "File operation failed. Please check the file and try again." }
        };

        public static void ShowError(UIError error, string customTitle = null)
        {
            var message = GetErrorMessage(error);
            var title = customTitle ?? "Error";
            
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowSuccess(string message, string title = "Success")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static string GetErrorMessage(UIError error)
        {
            if (ErrorMessages.TryGetValue(error.Code, out var defaultMessage))
            {
                return string.IsNullOrEmpty(error.Details) 
                    ? defaultMessage 
                    : $"{defaultMessage}\n\nDetails: {error.Details}";
            }

            return string.IsNullOrEmpty(error.Details)
                ? error.Message
                : $"{error.Message}\n\nDetails: {error.Details}";
        }

        public static string GetStatusMessage(UIError error)
        {
            return ErrorMessages.TryGetValue(error.Code, out var message) 
                ? message 
                : error.Message;
        }
    }
}
