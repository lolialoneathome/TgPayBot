using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public static class LogConst
    {
        // Listener consts
        public static string UserAlreadySubscribetButSendStart = "Пользователь, который уже подписан, повторно отправил сообщение /start";
        public static string UserRequestSubscribe = "Пользователь запрашивает подписку";
        public static string UserAlreadySubscribetButSendContact = "Пользователь, который уже авторизован, повторно отправил номер телефона";
        public static string UserNotSubscribedAndGetCodeButSendContact = "Пользователь, который еше не авторизован, но которому уже был отправлен код, повторно отправил номер телефона";
        public static string CodeSendedToUser = "Пользователю отправлен код";
        public static string UnsubscribedUserSendBye = "Пользователь, который не подписан, отправил сообщение /bye";
        public static string Unsubscribe = "Пользователь отписался";
        public static string MessageReceived = "Пришло сообщение";
        public static string CorrectCodeSuccessAuth = "Пользователь ввёл корректный код и успешно авторизовался";
        public static string IncorrectCode = "Пользователь ввел некорректный код";
        public static string UnsupportedTypeMessage = "Пришло сообщение неподдерживаемого типа";
        public static string RefreshCode = "Пользователь запросил код повторно";
        public static string RefreshCodeButYetSubscribed = "Пользователь запросил код повторно, но он уже авторизован в системе";
        public static string RefreshCodeButUnsubscribed = "Неизвестный пользователь отправил запрос на сброс кода";
        public static string AdminRequestUsers = "Запрос списка пользователей от администратора";
        public static string RequestUsersButPermissionDenied = "Попытка запросить пользователей из под учетки, у которой нет админских прав";
        public static string EnableSendingButItYetEnabled = "Попытка включить рассылку, которая уже работает";
        public static string SendingEnabled = "Рассылка возобновлена";
        public static string EnableSendingButPermissionDenied = "Попытка включить рассылку из под учетки, у которой нет прав";
        public static string DisableSendingButItYetDisabled = "Попытка выключить рассылку, которая уже остановлена";
        public static string SendingDisabled = "Рассылка остановлена";
        public static string DisableSendingButPermissionDenied = "Попытка выключить рассылку из под учетки, у которой нет прав";

        //Sender consts
        public static string SendedStopedDoNothing = "Рассылка остановлена, ничего не отправляю";
        public static string StartSending = "Начинаю отправку сообщений...";
        public static string InvalidConfig = "Invalid config file!";
    }
}
