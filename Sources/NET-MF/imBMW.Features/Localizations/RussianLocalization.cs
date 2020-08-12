﻿
using System;

namespace imBMW.Features.Localizations
{
    public class RussianLocalization : Localization
    {
        public const string SystemName = "Russian";

        public override string LanguageName
        {
            get { return "Русский"; }
        }

        public override string Language
        {
            get { return "Язык"; }
        }

        public override string Settings
        {
            get { return "Настройки"; }
        }

        public override string Bordcomputer
        {
            get { return "Борткомпьютер"; }
        }

        public override string BordcomputerShort
        {
            get { return "БК"; }
        }

        public override string Speed
        {
            get { return "Скорость"; }
        }

        public override string Revs
        {
            get { return "Обороты"; }
        }

        public override string Voltage
        {
            get { return "Напряжение"; }
        }

        public override string VoltageShort
        {
            get { return "В"; }
        }

        public override string Engine
        {
            get { return "Двигатель"; }
        }

        public override string Outside
        {
            get { return "Внеш. темп."; }
        }

        public override string Refreshing
        {
            get { return "Обновление"; }
        }

        public override string Player
        {
            get { return "Плеер"; }
        }

        public override string Phone
        {
            get { return "Телефон"; }
        }

        public override string QuickAccess
        {
            get { return "Быстрый доступ"; }
        }

        public override string ComfortWindows
        {
            get { return "Комфорт окна"; }
        }

        public override string ComfortSunroof
        {
            get { return "Комфорт люк"; }
        }

        public override string AutoLock
        {
            get { return "Авто запирание"; }
        }

        public override string AutoUnlock
        {
            get { return "Авто отпирание"; }
        }

        public override string VoiceCall
        {
            get { return "Голосовые команды"; }
        }

        public override string Contacts
        {
            get { return "Контакты"; }
        }

        public override string PrevItems
        {
            get { return "Предыдущие"; }
        }

        public override string NextItems
        {
            get { return "Следующие"; }
        }

        public override string Back
        {
            get { return "Назад"; }
        }

        public override string Volume
        {
            get { return "Громкость"; }
        }

        public override string Reconnect
        {
            get { return "Переподключение"; }
        }

        public override string Pair
        {
            get { return "Сопряжение"; }
        }

        public override string Playing
        {
            get { return "Играет"; }
        }

        public override string Paused
        {
            get { return "Пауза"; }
        }

        public override string Play
        {
            get { return "Играть"; }
        }

        public override string Pause
        {
            get { return "Пауза"; }
        }

        public override string PrevTrack
        {
            get { return "Предыдущий трек"; }
        }

        public override string NextTrack
        {
            get { return "Следующий трек"; }
        }

        public override string Connected
        {
            get { return "Подключен"; }
        }

        public override string Waiting
        {
            get { return "Ожидается"; }
        }

        public override string Disconnected
        {
            get { return "Отключен"; }
        }

        public override string NotPaired
        {
            get { return "Не спарен"; }
        }

        public override string Next
        {
            get { return "Следующий"; }
        }

        public override string Previous
        {
            get { return "Предыдущий"; }
        }

        public override string NoContacts
        {
            get { return "Нет контактов"; }
        }

        public override string Disconnect
        {
            get { return "Отключиться"; }
        }

        public override string Connect
        {
            get { return "Подключиться"; }
        }

        public override string NowPlaying
        {
            get { return "Сейчас играет"; }
        }

        public override string TrackTitle
        {
            get { return "Трек"; }
        }

        public override string Artist
        {
            get { return "Исполнитель"; }
        }

        public override string Album
        {
            get { return "Альбом"; }
        }

        public override string Genre
        {
            get { return "Жанр"; }
        }

        public override string Limit
        {
            get { return "Лимит"; }
        }

        public override string Range
        {
            get { return "Запас хода"; }
        }

        public override string Consumption
        {
            get { return "Расход"; }
        }

        public override string LitersPer100KM
        {
            get { return "л/100км"; }
        }

        public override string KM
        {
            get { return "км"; }
        }

        public override string KMH
        {
            get { return "км/ч"; }
        }

        public override string TurnOn
        {
            get { return "Включить"; }
        }

        public override string TurnOff
        {
            get { return "Выключить"; }
        }

        public override string LimitCurrentSpeed
        {
            get { return "Текущая скорость"; }
        }

        public override string LimitIncrease
        {
            get { return "Увеличить"; }
        }

        public override string LimitDecrease
        {
            get { return "Уменьшить"; }
        }

        public override string Average
        {
            get { return "Ср. скорость"; }
        }

        public override string AuxilaryHeater
        {
            get { return "Доп. отопитель"; }
        }

        public override string Activate
        {
            get { return "Активировать"; }
        }

        public override string Compressor
        {
            get { return "Компрессор"; }
        }

        public override string MusicList
        {
            get { return "Список музыки"; }
        }

        public override string AirConditioning
        {
            get { return "Блок климата"; }
        }

        public override string Temperature => "Температура";

        public override string Temp => "Темп.";

        public override string Oil => "Масло";
    }
}
