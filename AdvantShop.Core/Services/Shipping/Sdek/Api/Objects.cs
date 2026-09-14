using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AdvantShop.Shipping.Sdek.Api
{
    public class Error
    {
        /// <summary>
        /// Код ошибки
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Описание ошибки
        /// </summary>
        public string Message { get; set; }
    }

    #region Tariff

    public class TariffParams
    {
        /// <summary>
        /// Дата и время планируемой передачи заказа
        /// <para>По умолчанию - текущая</para>
        /// </summary>
        public DateTime? Date { get; set; }
        
        /// <summary>
        /// Тип заказа (для проверки доступности тарифа и дополнительных услуг по типу заказа):
        /// <para>1 - "интернет-магазин"</para>
        /// <para>2 - "доставка"</para>
        /// <para>По умолчанию - 1</para>
        /// </summary>
        public byte? Type { get; set; }
        
        /// <summary>
        /// Валюта, в которой необходимо произвести расчет
        /// <para>По умолчанию - валюта договора</para>
        /// </summary>
        public int? Currency { get; set; }
        
        /// <summary>
        /// Код тарифа
        /// </summary>
        public int TariffCode { get; set; }
        
        /// <summary>
        /// Адрес отправления
        /// </summary>
        public TariffParamsLocation FromLocation { get; set; }
        
        /// <summary>
        /// Адрес получения
        /// </summary>
        public TariffParamsLocation ToLocation { get; set; }
        
        /// <summary>
        /// Дополнительные услуги
        /// </summary>
        public List<ServiceParams> Services { get; set; }
        
        /// <summary>
        /// Список информации по местам (упаковкам)
        /// </summary>
        public List<TariffParamsPackage> Packages { get; set; }
    }
    
    
    public class TariffListParams
    {
        /// <summary>
        /// Дата и время планируемой передачи заказа
        /// <para>По умолчанию - текущая</para>
        /// </summary>
        public DateTime? Date { get; set; }
        
        /// <summary>
        /// Тип заказа (для проверки доступности тарифа и дополнительных услуг по типу заказа):
        /// <para>1 - "интернет-магазин"</para>
        /// <para>2 - "доставка"</para>
        /// </summary>
        public long? Type { get; set; }
        
        /// <summary>
        /// Валюта, в которой необходимо произвести расчет
        /// <para>По умолчанию - валюта договора</para>
        /// </summary>
        public long? Currency { get; set; }
        
        /// <summary>
        /// Язык вывода информации о тарифах
        /// </summary>
        public string Lang { get; set; }
        
        /// <summary>
        /// Адрес отправления
        /// </summary>
        public TariffParamsLocation FromLocation { get; set; }
        
        /// <summary>
        /// Адрес получения
        /// </summary>
        public TariffParamsLocation ToLocation { get; set; }
        
        /// <summary>
        /// Список информации по местам (упаковкам)
        /// </summary>
        public List<TariffParamsPackage> Packages { get; set; }
    }

    public class TariffParamsLocation
    {
        /// <summary>
        /// Код населенного пункта СДЭК
        /// </summary>
        public long? Code { get; set; }
        
        /// <summary>
        /// Почтовый индекс
        /// </summary>
        public string PostalCode { get; set; }
        
        /// <summary>
        /// Код страны в формате  ISO_3166-1_alpha-2
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Название города
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Полная строка адреса
        /// </summary>
        public string Address { get; set; }
    }

    public class TariffParamsPackage
    {
        /// <summary>
        /// Общий вес (в граммах)
        /// </summary>
        public long Weight { get; set; }

        /// <summary>
        /// Длина (в сантиметрах)
        /// </summary>
        public long? Length { get; set; }

        /// <summary>
        /// Ширина (в сантиметрах)
        /// </summary>
        public long? Width { get; set; }
        
        /// <summary>
        /// Высота (в сантиметрах)
        /// </summary>
        public long? Height { get; set; }
    }

    public class ServiceParams
    {
        /// <summary>
        /// Тип дополнительной услуги, код из справочника доп. услуг
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Параметр дополнительной услуги
        /// </summary>
        public string Parameter { get; set; }
    }
    
    
    
    public class TariffResult
    {
        /// <summary>
        /// Минимальное время доставки (в рабочих днях)
        /// </summary>
        public int PeriodMin { get; set; }
        
        /// <summary>
        /// Максимальное время доставки (в рабочих днях)
        /// </summary>
        public int PeriodMax { get; set; }
        
        /// <summary>
        /// Валюта, в которой рассчитана стоимость доставки
        /// </summary>
        public string Currency { get; set; }


        /// <summary>
        /// Стоимость доставки
        /// </summary>
        public float DeliverySum { get; set; }

        /// <summary>
        /// Расчетный вес (в граммах)
        /// </summary>
        public long WeightCalc { get; set; }
        
        /// <summary>
        /// Дополнительные услуги
        /// </summary>
        public List<TariffResultService> Services { get; set; }
        
        /// <summary>
        /// Стоимость доставки с учетом дополнительных услуг
        /// </summary>
        public float TotalSum { get; set; }
        
        public List<Error> Errors { get; set; }
    }

    public class TariffResultService
    {
        /// <summary>
        /// Тип дополнительной услуги, код из справочника доп. услуг
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Стоимость услуги
        /// </summary>
        public float Sum { get; set; }
    }
    
    public class TariffListResult
    {
        /// <summary>
        /// Доступные тарифы
        /// </summary>
        [JsonProperty("tariff_codes")]
        public List<TariffListResultTariff> Tariffs { get; set; }
        public List<Error> Errors { get; set; }
    }

    public class TariffListResultTariff
    {
        /// <summary>
        /// Код тарифа
        /// </summary>
        [JsonProperty("tariff_code")]
        public long Code { get; set; }
        
        /// <summary>
        /// Название тарифа на языке вывода
        /// </summary>
        [JsonProperty("tariff_name")]
        public string Name { get; set; }
        
        /// <summary>
        /// Описание тарифа на языке вывода
        /// </summary>
        [JsonProperty("tariff_description")]
        public string Description { get; set; }
        
        /// <summary>
        /// Режим тарифа
        /// </summary>
        public int DeliveryMode { get; set; }
        
        /// <summary>
        /// Стоимость доставки
        /// </summary>
        public float DeliverySum { get; set; }
        
        /// <summary>
        /// Минимальное время доставки (в рабочих днях)
        /// </summary>
        public int PeriodMin { get; set; }
        
        /// <summary>
        /// Максимальное время доставки (в рабочих днях)
        /// </summary>
        public int PeriodMax { get; set; }
    }
    
    #endregion

    #region DeliveryPoints

    public class DeliveryPointsFilter
    {
        /// <summary>
        /// Почтовый индекс города
        /// </summary>
        public int? PostalCode { get; set; }
        
        /// <summary>
        /// Код населенного пункта СДЭК
        /// </summary>
        public int? CityCode { get; set; }
        
        /// <summary>
        /// Тип офиса
        /// <para>PVZ - для отображения только складов</para>
        /// <para>POSTAMAT - для отображения постаматов</para>
        /// <para>ALL - для отображения всех ПВЗ независимо от их типа</para>
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Код страны в формате ISO_3166-1_alpha-2
        /// </summary>
        public string CountryCode { get; set; }
        
        /// <summary>
        /// Код региона по базе СДЭК
        /// </summary>
        public string RegionCode { get; set; }
        
        /// <summary>
        /// Наличие терминала оплаты
        /// </summary>
        public bool? HaveCashless { get; set; }
        
        /// <summary>
        /// Есть прием наличных
        /// </summary>
        public bool? HaveCash { get; set; }
        
        /// <summary>
        /// Разрешен наложенный платеж
        /// </summary>
        public bool? AllowedCod { get; set; }
        
        /// <summary>
        /// Наличие примерочной
        /// </summary>
        public bool? IsDressingRoom { get; set; }
        
        /// <summary>
        /// Максимальный вес в кг, который может принять офис
        /// </summary>
        public int? WeightMax { get; set; }
        
        /// <summary>
        /// Минимальный вес в кг, который принимает офис
        /// </summary>
        public int? WeightMin { get; set; }
        
        /// <summary>
        /// Локализация офиса.
        /// <para>По умолчанию "rus".</para>
        /// </summary>
        public string Lang { get; set; }
        
        /// <summary>
        /// Является ли офис только пунктом выдачи
        /// </summary>
        public bool? TakeOnly { get; set; }
        
        /// <summary>
        /// Является пунктом выдачи
        /// </summary>
        public bool? IsHandout { get; set; }
        
        /// <summary>
        /// Есть ли в офисе приём заказов
        /// </summary>
        public bool? IsReception { get; set; }
    }
    
    public class DeliveryPoint
    {
        /// <summary>
        /// Код
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Адрес офиса
        /// </summary>
        public DeliveryPointLocation Location { get; set; }
        
        /// <summary>
        /// Описание местоположения
        /// </summary>
        public string AddressComment { get; set; }
        
        /// <summary>
        /// Ближайшая станция/остановка транспорта
        /// </summary>
        public string NearestStation { get; set; }
        
        /// <summary>
        /// Ближайшая станция метро
        /// </summary>
        public string NearestMetroStation { get; set; }
        
        /// <summary>
        /// Режим работы, строка вида «пн-пт 9-18, сб 9-16»
        /// </summary>
        public string WorkTime { get; set; }
        
        /// <summary>
        /// Список телефонов
        /// </summary>
        public List<Phone> Phones { get; set; }
        
        /// <summary>
        /// Адрес электронной почты
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Примечание по офису
        /// </summary>
        public string Note { get; set; }
        
        /// <summary>
        /// Тип ПВЗ
        /// <para>PVZ — склад</para>
        /// <para>POSTAMAT — постамат</para>
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Принадлежность офиса компании
        /// <para>cdek — офис принадлежит компании СДЭК</para>
        /// <para>InPost — офис принадлежит компании InPost</para>
        /// </summary>
        public string OwnerСode { get; set; }
        
        /// <summary>
        /// Является ли офис только пунктом выдачи или также осуществляет приём грузов
        /// </summary>
        public bool TakeOnly { get; set; }
               
        /// <summary>
        /// Является пунктом выдачи
        /// </summary>
        public bool IsHandout { get; set; }
                
        /// <summary>
        /// Есть ли в офисе приём заказов
        /// </summary>
        public bool IsReception { get; set; }
        
        /// <summary>
        /// Есть ли примерочная
        /// </summary>
        public bool IsDressingRoom { get; set; }
        
        /// <summary>
        /// Есть безналичный расчет
        /// </summary>
        public bool HaveCashless { get; set; }
        
        /// <summary>
        /// Есть приём наличных
        /// </summary>
        public bool HaveCash { get; set; }
        
        /// <summary>
        /// Разрешен наложенный платеж в ПВЗ
        /// </summary>
        public bool AllowedCod { get; set; }
        
        /// <summary>
        /// Ссылка на данный офис на сайте СДЭК
        /// </summary>
        public string Site { get; set; }
        
        /// <summary>
        /// График работы на неделю
        /// </summary>
        public List<WorkTimeList> WorkTimeList { get; set; }
        
        /*
        /// <summary>
        /// Все фото офиса (кроме фото как доехать).
        /// </summary>
        public List<OfficeImageList> OfficeImageList { get; set; }
        
        /// <summary>
        /// Исключения в графике работы офиса
        /// </summary>
        public List<WorkTimeException> WorkTimeExceptions { get; set; }
        */
        
        /// <summary>
        /// Минимальный вес (в кг.), принимаемый в ПВЗ (> WeightMin)
        /// </summary>
        public float? WeightMin { get; set; }
        
        /// <summary>
        /// Максимальный вес (в кг.), принимаемый в ПВЗ (&lt;=WeightMax)
        /// </summary>
        public float? WeightMax { get; set; }
        
        /// <summary>
        /// Наличие зоны фулфилмента
        /// </summary>
        public bool Fulfillment { get; set; }
        
        /// <summary>
        /// Перечень максимальных размеров ячеек (только для type = POSTAMAT)
        /// </summary>
        public List<DeliveryPointDimensions> Dimensions { get; set; }
    }

    public class DeliveryPointLocation
    {
        /// <summary>
        /// Код населенного пункта СДЭК
        /// </summary>
        public int CityCode { get; set; }
        
        /*
        /// <summary>
        /// Код страны в формате ISO_3166-1_alpha-2
        /// </summary>
        public string CountryCode { get; set; }
        
        /// <summary>
        /// Код региона СДЭК
        /// </summary>
        public int RegionCode { get; set; }
        
        /// <summary>
        /// Название региона
        /// </summary>
        public string Region { get; set; }
        
        /// <summary>
        /// Название города
        /// </summary>
        public string City { get; set; }
        
        /// <summary>
        /// Почтовый индекс
        /// </summary>
        public string PostalCode { get; set; }
        */
        
        /// <summary>
        /// Координаты местоположения (долгота) в градусах
        /// </summary>
        public float Longitude { get; set; }
        
        /// <summary>
        /// Координаты местоположения (широта) в градусах
        /// </summary>
        public float Latitude { get; set; }
        
        /// <summary>
        /// Адрес (улица, дом, офис) в указанном городе
        /// </summary>
        public string Address { get; set; }
        
        /// <summary>
        /// Полный адрес с указанием страны, региона, города, и т.д.
        /// </summary>
        public string AddressFull { get; set; }
        
        /// <summary>
        /// Код города ФИАС
        /// </summary>
        public string FiasGuid { get; set; }
    }

    public class Phone
    {
        /// <summary>
        /// Номер телефона
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Дополнительная информация (доп. номер)
        /// </summary>
        public string Additional { get; set; }
    }

    
    public class OfficeImageList
    {
        /// <summary>
        /// Ссылка на фото
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Номер фото
        /// </summary>
        public int Number { get; set; }
    }
    public class WorkTimeException
    {
        /// <summary>
        /// Дата
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Период работы в указанную дату. Если в этот день не работают, то не отображается.
        /// </summary>
        public string Time { get; set; }
        
        /// <summary>
        /// Признак рабочего/нерабочего дня в указанную дату
        /// </summary>
        public bool IsWorking { get; set; }
    }

    public class WorkTimeList
    {
        /// <summary>
        /// Порядковый номер дня начиная с единицы. Понедельник = 1, воскресенье = 7.
        /// </summary>
        public byte Day { get; set; }
        
        /// <summary>
        /// Период работы в эти дни. Если в этот день не работают, то не отображается.
        /// </summary>
        public string Time { get; set; }
    }

    public class DeliveryPointDimensions
    {
        /// <summary>
        /// Ширина (см)
        /// </summary>
        public float Width { get; set; }
        
        /// <summary>
        /// Высота (см)
        /// </summary>
        public float Height { get; set; }
        
        /// <summary>
        /// Глубина (см)
        /// </summary>
        public float Depth { get; set; }
    }

    #endregion

    #region OAuth

    public class OAuthData
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public long? ExpiresIn { get; set; }
        public string Scope { get; set; }
        public string Jti { get; set; }
    }

    #endregion

    #region Order

    public class NewOrder
    {
        /// <summary>
        /// Тип заказа (для проверки доступности тарифа и дополнительных услуг по типу заказа):
        /// <para>1 - "интернет-магазин"</para>
        /// <para>2 - "доставка"</para>
        /// <para>По умолчанию - 1</para>
        /// </summary>
        public byte? Type { get; set; }
        
        /// <summary>
        /// Номер заказа в ИС Клиента
        /// <para>Только для заказов "интернет-магазин"</para>
        /// <remarks>Может содержать только цифры, буквы латинского алфавита или спецсимволы (формат ASCII)</remarks>
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Код тарифа
        /// </summary>
        public int TariffCode { get; set; }
        
        /// <summary>
        /// Комментарий к заказу
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// Ключ разработчика
        /// </summary>
        public string DeveloperKey { get; set; }
        
        /// <summary>
        /// Код ПВЗ СДЭК, на который будет производиться самостоятельный привоз клиентом
        /// <remarks>Не может использоваться одновременно с FromLocation</remarks>
        /// </summary>
        public string ShipmentPoint { get; set; }
        
        /// <summary>
        /// Код ПВЗ СДЭК, на который будет доставлена посылка
        /// <remarks>Не может использоваться одновременно с ToLocation</remarks>
        /// </summary>
        public string DeliveryPoint { get; set; }
        
        /// <summary>
        /// Дата инвойса
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter), "yyyy-MM-dd")]
        public DateTime? DateInvoice { get; set; }
        
        /// <summary>
        /// Грузоотправитель
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        public string ShipperName { get; set; }
        
        /// <summary>
        /// Адрес грузоотправителя
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        public string ShipperAddress { get; set; }
        
        /// <summary>
        /// Доп. сбор за доставку, которую ИМ берет с получателя.
        /// <remarks>Только для заказов "интернет-магазин"</remarks>
        /// </summary>
        public MoneyParams DeliveryRecipientCost { get; set; }
        
        /// <summary>
        /// Доп. сбор за доставку (которую ИМ берет с получателя) в зависимости от суммы заказа
        /// <remarks>Только для заказов "интернет-магазин"</remarks>
        /// </summary>
        public List<DeliveryRecipientCostAdv> DeliveryRecipientCostAdv { get; set; }

        /// <summary>
        /// Отправитель
        /// <para>Обязателен:</para>
        /// <para>нет, если заказ типа "интернет-магазин"</para>
        /// <para>да, если заказ типа "доставка"</para>
        /// </summary>
        public Sender Sender { get; set; }
        
        /// <summary>
        /// Реквизиты истинного продавца
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        public Seller Seller { get; set; }

        /// <summary>
        /// Получатель
        /// <remarks>Обязательный</remarks>
        /// </summary>
        public Recipient Recipient { get; set; }
        
        /// <summary>
        /// Адрес отправления
        /// <remarks>Не может использоваться одновременно с ShipmentPoint
        /// <para>Обязательно если заказ с тарифом "от двери"</para></remarks>
        /// </summary>
        public OrderLocation FromLocation { get; set; }
        
        /// <summary>
        /// Адрес получения
        /// <remarks>Не может использоваться одновременно с DeliveryPoint
        /// <para>Обязательно если заказ с тарифом "до двери"</para></remarks>
        /// </summary>
        public OrderLocation ToLocation { get; set; }

        /// <summary>
        /// Дополнительные услуги
        /// </summary>
        public List<ServiceParams> Services { get; set; }
        
        /// <summary>
        /// Список информации по местам
        /// <remarks>Количество мест в заказе может быть от 1 до 255
        /// <para>Обязательно</para></remarks>
        /// </summary>
        public List<OrderPackage> Packages { get; set; }
    }

    public class MoneyParams
    {
        /// <summary>
        /// Сумма дополнительного сбора
        /// </summary>
        public float Value { get; set; }
        
        /// <summary>
        /// Сумма НДС
        /// </summary>
        public float? VatSum { get; set; }
        
        /// <summary>
        /// Ставка НДС
        /// <remarks>(значение - 0, 10, 18, 20 и т.п. , null - нет НДС)</remarks>
        /// </summary>
        public int? VatRate { get; set; }
    }

    public class DeliveryRecipientCostAdv
    {
        /// <summary>
        /// Порог стоимости товара (действует по условию меньше или равно) в целых единицах валюты
        /// </summary>
        public int Threshold { get; set; }
        
        /// <summary>
        /// Доп. сбор за доставку товаров, общая стоимость которых попадает в интервал
        /// </summary>
        public float Sum { get; set; }
            
        /// <summary>
        /// Сумма НДС, включённая в доп. сбор за доставку
        /// </summary>
        public float? VatSum { get; set; }
        
        /// <summary>
        /// Ставка НДС
        /// <remarks>(значение - 0, 10, 18, 20 и т.п. , null - нет НДС)</remarks>
        /// </summary>
        public int? VatRate { get; set; }
    }

    public class OrderLocation
    {
        /// <summary>
        /// Код населенного пункта СДЭК
        /// </summary>
        public int? Code { get; set; }
        
        /// <summary>
        /// Уникальный идентификатор ФИАС
        /// </summary>
        public Guid? FiasGuid { get; set; }
        
        /// <summary>
        /// Почтовый индекс
        /// </summary>
        public string PostalCode { get; set; }
        
        /// <summary>
        /// Долгота
        /// </summary>
        public float? Longitude { get; set; }
        
        /// <summary>
        /// Широта
        /// </summary>
        public float? Latitude { get; set; }
        
        /// <summary>
        /// Код страны в формате  ISO_3166-1_alpha-2
        /// </summary>
        public string CountryCode { get; set; }
        
        /// <summary>
        /// Название региона
        /// </summary>
        public string Region { get; set; }
        
        /// <summary>
        /// Код региона СДЭК
        /// </summary>
        public int? RegionCode { get; set; }
        
        /// <summary>
        /// Название района региона
        /// </summary>
        public string SubRegion { get; set; }
        
        /// <summary>
        /// Название города
        /// </summary>
        public string City { get; set; }
        
        /// <summary>
        /// Код КЛАДР
        /// </summary>
        [Obsolete]
        public string KladrCode { get; set; }
        
        /// <summary>
        /// Строка адреса
        /// </summary>
        public string Address { get; set; }
    }

    public class OrderPackage
    {
        /// <summary>
        /// Номер упаковки
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Общий вес (в граммах)
        /// </summary>
        public long Weight { get; set; }

        /// <summary>
        /// Длина (в сантиметрах)
        /// <remarks>Обязателен если:
        /// <para>если указаны остальные габариты</para>
        /// <para>если заказ до постамата</para>
        /// <para>если общий вес >=100 гр</para></remarks>
        /// </summary>
        public long? Length { get; set; }

        /// <summary>
        /// Ширина (в сантиметрах)
        /// <remarks>Обязателен если:
        /// <para>если указаны остальные габариты</para>
        /// <para>если заказ до постамата</para>
        /// <para>если общий вес >=100 гр</para></remarks>
        /// </summary>
        public long? Width { get; set; }

        /// <summary>
        /// Высота (в сантиметрах)
        /// <remarks>Обязателен если:
        /// <para>если указаны остальные габариты</para>
        /// <para>если заказ до постамата</para>
        /// <para>если общий вес >=100 гр</para></remarks>
        /// </summary>
        public long? Height { get; set; }
        
        /// <summary>
        /// Комментарий к упаковке
        /// <remarks>Обязательно и только для заказа типа "доставка"</remarks>
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// Позиции товаров в упаковке
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Максимум 126 уникальных позиций</para>
        /// <para>Обязательно</para></remarks>
        /// </summary>
        public List<OrderPackageItem> Items { get; set; }
    }

    public class OrderPackageItem
    {
        /// <summary>
        /// Наименование товара (может также содержать описание товара: размер, цвет)
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Идентификатор/артикул товара
        /// <remarks>Артикул товара может содержать только символы: [A-z А-я 0-9 ! @ " # № $ ; % ^ : & ? * () _ - + = ? < > , .{ } [ ] \ / , пробел]</remarks>
        /// </summary>
        public string WareKey { get; set; }
        
        /// <summary>
        /// Маркировка товара
        /// </summary>
        public string Marking { get; set; }
        
        /// <summary>
        /// Оплата за товар при получении (за единицу товара в валюте страны получателя, значение >=0)
        /// <remarks>наложенный платеж, в случае предоплаты значение = 0</remarks>
        /// </summary>
        public MoneyParams Payment { get; set; }
        
        /// <summary>
        /// Объявленная стоимость товара (за единицу товара в валюте взаиморасчетов, значение >=0).
        /// <remarks>С данного значения рассчитывается страховка</remarks>
        /// </summary>
        public float Cost { get; set; }

        /// <summary>
        /// Вес (за единицу товара, в граммах)
        /// </summary>
        public long Weight { get; set; }
        
        /// <summary>
        /// Вес брутто
        /// <remarks>Обязательный если заказ международный</remarks>
        /// </summary>
        public int? WeightGross { get; set; }
        
        /// <summary>
        /// Количество единиц товара (в штуках)
        /// <remarks>Количество одного товара в заказе может быть от 1 до 999</remarks>
        /// </summary>
        public int Amount { get; set; }
        
        /// <summary>
        /// Наименование на иностранном языке
        /// </summary>
        public string NameI18n { get; set; }
        
        /// <summary>
        /// Бренд на иностранном языке
        /// </summary>
        public string Brand { get; set; }
        
        /// <summary>
        /// Код страны производителя товара в формате  ISO_3166-1_alpha-2
        /// </summary>
        public string CountryCode { get; set; }
        
        public EnMaterial? Material { get; set; }
        
        /// <summary>
        /// Содержит wifi/gsm
        /// </summary>
        public bool? WifiGsm { get; set; }
        
        /// <summary>
        /// Ссылка на сайт интернет-магазина с описанием товара
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Необходимость сформировать печатную форму по заказу
        /// <remarks>Может принимать значения:
        /// <para>barcode - ШК мест (число копий - 1)</para>
        /// <para>waybill - квитанция (число копий - 2)</para></remarks>
        /// </summary>
        public string Print { get; set; }
    }

    public enum EnMaterial
    {
        /// <summary>
        /// Полиэстер
        /// </summary>
        Polyester = 1,
        
        /// <summary>
        /// Нейлон
        /// </summary>
        Nylon = 2,
        
        /// <summary>
        /// Флис
        /// </summary>
        Fleece = 3,
        
        /// <summary>
        /// Хлопок
        /// </summary>
        Cotton = 4,
        
        /// <summary>
        /// Текстиль
        /// </summary>
        Textiles = 5,
        
        /// <summary>
        /// Лён
        /// </summary>
        Flax = 6,
        
        /// <summary>
        /// Вискоза
        /// </summary>
        Viscose = 7,
        
        /// <summary>
        /// Шелк
        /// </summary>
        Silk = 8,
        
        /// <summary>
        /// Шерсть
        /// </summary>
        Wool = 9,
        
        /// <summary>
        /// Кашемир
        /// </summary>
        Cashmere = 10,
        
        /// <summary>
        /// Кожа
        /// </summary>
        Leather = 11,
        
        /// <summary>
        /// Кожзам
        /// </summary>
        Leatherette = 12,
        
        /// <summary>
        /// Искусственный мех
        /// </summary>
        ArtificialFur = 13,
        
        /// <summary>
        /// Замша
        /// </summary>
        Suede = 14,
        
        /// <summary>
        /// Полиуретан
        /// </summary>
        Polyurethane = 15,
        
        /// <summary>
        /// Спандекс
        /// </summary>
        Spandex = 16,
        
        /// <summary>
        /// Резина
        /// </summary>
        Rubber = 17,
    }

    public class Recipient
    {
        /// <summary>
        /// Название компании
        /// </summary>
        public string Company { get; set; }
        
        /// <summary>
        /// ФИО контактного лица
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Серия паспорта
        /// </summary>
        public string PassportSeries { get; set; }
        
        /// <summary>
        /// Номер паспорта
        /// </summary>
        public string PassportNumber { get; set; }
        
        /// <summary>
        /// Дата выдачи паспорта
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter), "yyyy-MM-dd")]
        public DateTime? PassportDateOfIssue { get; set; }
        
        /// <summary>
        /// Орган выдачи паспорта
        /// </summary>
        public string PassportOrganization { get; set; }
        
        /// <summary>
        /// ИНН
        /// <remarks>Может содержать 10, либо 12 символов</remarks>
        /// </summary>
        public string Tin { get; set; }
        
        /// <summary>
        /// Дата выдачи паспорта
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter), "yyyy-MM-dd")]
        public DateTime? PassportDateOfBirth { get; set; }
        
        public string Email { get; set; }
    
        /// <summary>
        /// Список телефонов
        /// <remarks>Не более 10 номеров
        /// <para>Обязательный</para></remarks>
        /// </summary>
        public List<Phone> Phones { get; set; }
    }

    public class Sender
    {
        /// <summary>
        /// Название компании
        /// </summary>
        public string Company { get; set; }
        
        /// <summary>
        /// ФИО контактного лица
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Эл. адрес
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Список телефонов
        /// <remarks>Не более 10 номеров</remarks>
        /// </summary>
        public List<Phone> Phones { get; set; }
    }

    public class Seller
    {
        /// <summary>
        /// Наименование истинного продавца
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// ИНН истинного продавца
        /// <remarks>Может содержать 10, либо 12 символов</remarks>
        /// </summary>
        public string Inn { get; set; }
        
        /// <summary>
        /// Телефон истинного продавца
        /// </summary>
        public string Phone { get; set; }
        
        public EnOwnershipForm? OwnershipForm { get; set; }
        
        /// <summary>
        /// Адрес истинного продавца
        /// </summary>
        public string Address { get; set; }
    }

    public enum EnOwnershipForm
    {
        /// <summary>
        /// Акционерное общество
        /// </summary>
        AO = 9,
        
        /// <summary>
        /// Закрытое акционерное общество
        /// </summary>
        ZAO = 61,
        
        /// <summary>
        /// Индивидуальный предприниматель
        /// </summary>
        IP = 63,
        
        /// <summary>
        /// Открытое акционерное общество
        /// </summary>
        OAO = 119,
        
        /// <summary>
        /// Общество с ограниченной ответственностью
        /// </summary>
        OOO = 137,
        
        /// <summary>
        /// Публичное акционерное общество
        /// </summary>
        PAO = 147,
    }

    public class NewOrderResult
    {
        /// <summary>
        /// Информация о заказе
        /// </summary>
        public Entity Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе над заказом
        /// </summary>
        public List<RequestInfoNew> Requests { get; set; }
    }

    public class Entity
    {
        /// <summary>
        /// Идентификатор заказа в ИС СДЭК
        /// </summary>
        public Guid? Uuid { get; set; }
    }

    public class RequestInfoNew : RequestInfo
    {

        /// <summary>
        /// Связанные сущности (если в запросе был передан корректный print)
        /// </summary>
        private RelatedEntity RelatedEntities { get; set; }
    }

    public class RelatedEntity
    {
        /// <summary>
        /// Тип связанной сущности
        /// <remarks>Может принимать значения:
        /// <para>barcode - ШК места к заказу</para>
        /// <para>waybill - квитанция к заказу</para></remarks>
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Идентификатор сущности, связанной с заказом
        /// </summary>
        public Guid Uuid { get; set; }
    }

    public class GetOrderResult
    {
        /// <summary>
        /// Информация о заказе
        /// </summary>
        public SdekOrder Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе/запросах над заказом
        /// </summary>
        public List<RequestInfo> Requests { get; set; }
        
        /// <summary>
        /// Связанные с заказом сущности
        /// </summary>
        public List<RelatedEntity> RelatedEntities { get; set; }
    }

    public class SdekOrder
    {
        /// <summary>
        /// Идентификатор заказа в ИС СДЭК
        /// </summary>
        public Guid Uuid { get; set; }
        
        /// <summary>
        /// Признак возвратного заказа
        /// </summary>
        public bool IsReturn { get; set; }
        
        /// <summary>
        /// Признак реверсного заказа
        /// </summary>
        public bool IsReverse { get; set; }
        
        /// <summary>
        /// Номер заказа СДЭК
        /// </summary>
        public string CdekNumber { get; set; }
        
        /// <summary>
        /// Истинный режим заказа
        /// <remarks>1 - дверь-дверь
        /// <para>2 - дверь-склад</para>
        /// <para>3 - склад-дверь</para>
        /// <para>4 - склад-склад</para>
        /// <para>6 - дверь-постамат</para>
        /// <para>7 - склад-постамат</para>
        /// </remarks>
        /// </summary>
        public string DeliveryMode { get; set; }
        
        /// <summary>
        /// <remarks>недокументированно</remarks>
        /// </summary>
        public string RecipientCurrency { get; set; }
        
        /// <summary>
        /// <remarks>недокументированно</remarks>
        /// </summary>
        public string ItemsCostCurrency { get; set; }
        
        /// <summary>
        /// <remarks>недокументированно</remarks>
        /// </summary>
        public string ShopSellerName { get; set; }
        
        public List<SdekOrderStatus> Statuses { get; set; }
        public List<Error> Errors { get; set; }

        /// <summary>
        /// Тип заказа:
        /// <para>1 - "интернет-магазин"</para>
        /// <para>2 - "доставка"</para>
        /// </summary>
        public byte Type { get; set; }
        
        /// <summary>
        /// Номер заказа в ИС Клиента
        /// <para>Только для заказов "интернет-магазин"</para>
        /// <remarks>Может содержать только цифры, буквы латинского алфавита или спецсимволы (формат ASCII)</remarks>
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Код тарифа
        /// </summary>
        public int TariffCode { get; set; }
        
        /// <summary>
        /// Комментарий к заказу
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// Ключ разработчика
        /// </summary>
        public string DeveloperKey { get; set; }
        
        /// <summary>
        /// Код ПВЗ СДЭК, на который будет производиться самостоятельный привоз клиентом
        /// <remarks>Не может использоваться одновременно с FromLocation</remarks>
        /// </summary>
        public string ShipmentPoint { get; set; }
        
        /// <summary>
        /// Код ПВЗ СДЭК, на который будет доставлена посылка
        /// <remarks>Не может использоваться одновременно с ToLocation</remarks>
        /// </summary>
        public string DeliveryPoint { get; set; }
        
        /// <summary>
        /// Дата инвойса
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter), "yyyy-MM-dd")]
        public DateTime? DateInvoice { get; set; }
        
        /// <summary>
        /// Грузоотправитель
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        public string ShipperName { get; set; }
        
        /// <summary>
        /// Адрес грузоотправителя
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        public string ShipperAddress { get; set; }
        
        /// <summary>
        /// Доп. сбор за доставку, которую ИМ берет с получателя.
        /// <remarks>Только для заказов "интернет-магазин"</remarks>
        /// </summary>
        public MoneyParams DeliveryRecipientCost { get; set; }
        
        /// <summary>
        /// Доп. сбор за доставку (которую ИМ берет с получателя) в зависимости от суммы заказа
        /// <remarks>Только для заказов "интернет-магазин"</remarks>
        /// </summary>
        public List<DeliveryRecipientCostAdv> DeliveryRecipientCostAdv { get; set; }

        /// <summary>
        /// Отправитель
        /// <para>Обязателен:</para>
        /// <para>нет, если заказ типа "интернет-магазин"</para>
        /// <para>да, если заказ типа "доставка"</para>
        /// </summary>
        public SenderSdekOrder Sender { get; set; }
        
        /// <summary>
        /// Реквизиты истинного продавца
        /// <remarks>Только для заказов "интернет-магазин"
        /// <para>Обязательно если заказ международный</para></remarks>
        /// </summary>
        public Seller Seller { get; set; }

        /// <summary>
        /// Получатель
        /// <remarks>Обязательный</remarks>
        /// </summary>
        public RecipientSdekOrder Recipient { get; set; }
        
        /// <summary>
        /// Адрес отправления
        /// <remarks>Не может использоваться одновременно с ShipmentPoint
        /// <para>Обязательно если заказ с тарифом "от двери"</para></remarks>
        /// </summary>
        public OrderLocation FromLocation { get; set; }
        
        /// <summary>
        /// Адрес получения
        /// <remarks>Не может использоваться одновременно с DeliveryPoint
        /// <para>Обязательно если заказ с тарифом "до двери"</para></remarks>
        /// </summary>
        public OrderLocation ToLocation { get; set; }

        /// <summary>
        /// Дополнительные услуги
        /// </summary>
        public List<ServiceParamsSdekOrder> Services { get; set; }
        
        /// <summary>
        /// Список информации по местам
        /// <remarks>Количество мест в заказе может быть от 1 до 255
        /// <para>Обязательно</para></remarks>
        /// </summary>
        public List<OrderPackageSdekOrder> Packages { get; set; }
        
        /// <summary>
        /// Проблемы доставки
        /// </summary>
        public List<DeliveryProblem> DeliveryProblem { get; set; }
        
        /// <summary>
        /// Информация о вручении
        /// </summary>
        public DeliveryDetail DeliveryDetail { get; set; }
        
        /// <summary>
        /// Признак того, что по заказу была получена информация о переводе наложенного платежа интернет-магазину
        /// </summary>
        public bool TransactedPayment { get; set; }

        [JsonConverter(typeof(CustomDateTimeConverter), "yyyy-MM-dd")]
        public DateTime? KeepFreeUntil { get; set; }//GlorySoft_001
    }

    public class SenderSdekOrder : Sender
    {
        /// <summary>
        /// Требования по паспортным данным удовлетворены
        /// <remarks>актуально для международных заказов</remarks>
        /// </summary>
        public bool? PassportRequirementsSatisfied { get; set; }
    }

    public class RecipientSdekOrder : Recipient
    {
        /// <summary>
        /// Требования по паспортным данным удовлетворены
        /// <remarks>актуально для международных заказов</remarks>
        /// </summary>
        public bool? PassportRequirementsSatisfied { get; set; }
    }

    public class ServiceParamsSdekOrder : ServiceParams
    {
        /// <summary>
        /// Стоимость услуги
        /// <remarks>в валюте взаиморасчетов</remarks>
        /// </summary>
        public float? Sum { get; set; }
    }

    public class OrderPackageSdekOrder : OrderPackage
    {
        public string PackageId { get; set; }
    
        /// <summary>
        /// Объемный вес (в граммах)
        /// </summary>
        public long? WeightVolume { get; set; }
     
        /// <summary>
        /// Расчетный вес (в граммах)
        /// </summary>
        public long? WeightCalc { get; set; }
    }

    public class DeliveryProblem
    {
        public string Code { get; set; }
        public DateTime? CreateDate { get; set; }
    }

    public class DeliveryDetail
    {
        /// <summary>
        /// Дата доставки
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Получатель при доставке
        /// </summary>
        public string RecipientName { get; set; }
        
        /// <summary>
        /// Сумма наложенного платежа, которую взяли с получателя, в валюте страны получателя с учетом частичной доставки
        /// </summary>
        public float? PaymentSum { get; set; }
        
        /// <summary>
        /// Тип оплаты наложенного платежа получателем
        /// </summary>
        public List<PaymentInfo> PaymentInfo { get; set; }
        
        /// <summary>
        /// Стоимость услуги доставки
        /// </summary>
        public float DeliverySum { get; set; }
        
        /// <summary>
        /// Итоговая стоимость заказа
        /// </summary>
        public float TotalSum { get; set; }
    }

    public class PaymentInfo
    {
        /// <summary>
        /// Тип оплаты
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Сумма в валюте страны получателя
        /// </summary>
        public float Sum { get; set; }
    }

    public class SdekOrderStatus
    {
        /// <summary>
        /// Код статуса
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Название статуса
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Дата и время установки статуса
        /// </summary>
        public DateTime DateTime { get; set; }
        
        /// <summary>
        /// Дополнительный код статуса
        /// </summary>
        public string ReasonCode { get; set; }
        
        /// <summary>
        /// Наименование места возникновения статуса
        /// </summary>
        public string City { get; set; }
    }

    public class RequestInfo
    {
        /// <summary>
        /// Идентификатор запроса в ИС СДЭК
        /// </summary>
        public Guid? RequestUuid { get; set; }
        
        /// <summary>
        /// Тип запроса
        /// <remarks>Может принимать значения: CREATE, UPDATE, DELETE, AUTH, GET</remarks>
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Текущее состояние запроса
        /// <remarks>Может принимать значения
        /// <para>ACCEPTED - пройдена предварительная валидация и запрос принят</para>
        /// <para>WAITING - запрос ожидает обработки (зависит от выполнения другого запроса)</para>
        /// <para>SUCCESSFUL - запрос обработан успешно</para>
        /// <para>INVALID - запрос обработался с ошибкой</para>
        /// </remarks>
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// Дата и время установки текущего состояния запроса
        /// </summary>
        public string DateTime { get; set; }
        
        /// <summary>
        /// Ошибки, возникшие в ходе выполнения запроса
        /// </summary>
        public List<Error> Errors { get; set; }
        
        /// <summary>
        /// Предупреждения, возникшие в ходе выполнения запроса
        /// </summary>
        public List<Error> Warnings { get; set; }

    }

    public class EntityUuid
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Uuid { get; set; }
    }

    public class DeleteOrderResult
    {
        /// <summary>
        /// Информация о заказе
        /// </summary>
        public EntityUuid Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе/запросах над заказом
        /// </summary>
        public List<RequestInfo> Requests { get; set; }
    }
    
    #endregion

    #region CreatePrintForm

    public class CreatePrintForm
    {
        /// <summary>
        /// Список заказов
        /// </summary>
        public List<CreatePrintFormOrder> Orders { get; set; }
        
        /// <summary>
        /// Число копий одной квитанции на листе
        /// <remarks>По умолчанию 2</remarks>
        /// </summary>
        public int? CopyCount { get; set; }
        
        /// <summary>
        /// Форма квитанции.
        /// <remarks>Может принимать значения:
        /// <para>tpl_china - квитанция на китайском</para>
        /// <para>tpl_armenia - квитанция на армянском</para>
        /// <para>По умолчанию будет выбрана форма в зависимости от типа заказа</para></remarks>
        /// </summary>
        public string Type { get; set; }
    }

    public class CreatePrintFormOrder
    {
        public Guid? OrderUuid { get; set; }
        public long? CdekNumber { get; set; }
    }

    public class CreatePrintFormResult
    {
        /// <summary>
        /// Информация о квитанции к заказу
        /// </summary>
        public EntityUuid Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе/запросах
        /// </summary>
        public List<RequestInfo> Requests { get; set; }
    }

    #endregion

    #region GetPrintForm

    public class GetPrintFormResult
    {
        /// <summary>
        /// Информация о квитанции к заказу
        /// </summary>
        public EntityGetPrintForm Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе над квитанцией к заказу
        /// </summary>
        public List<RequestInfo> Requests { get; set; }
    }

    public class EntityGetPrintForm : EntityUuid
    {
        public List<CreatePrintFormOrder> Orders { get; set; }
            
        /// <summary>
        /// Число копий одной квитанции на листе
        /// </summary>
        public int? CopyCount { get; set; }
        
        /// <summary>
        /// Форма квитанции.
        /// </summary>
        public string Type { get; set; }
  
        /// <summary>
        /// Ссылка на скачивание файла
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Статус квитанции
        /// </summary>
        public List<FileStatus> Statuses { get; set; }
    }
    
    public class FileStatus
    {
        /// <summary>
        /// Код статуса
        /// <remarks>Может принимать следующие значения:
        /// <para>ACCEPTED	Принят	Запрос на формирование квитанции принят</para>
        /// <para>PROCESSING	Формируется	Файл с квитанцией формируется</para>
        /// <para>READY	Сформирован	Файл с квитанцией и ссылка на скачивание файла сформированы</para>
        /// <para>REMOVED	Удален	Истекло время жизни ссылки на скачивание файла с квитанцией</para>
        /// <para>INVALID	Некорректный запрос	Некорректный запрос на формирование квитанции</para>
        /// </remarks>
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Название статуса
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Дата и время установки статуса
        /// </summary>
        public DateTime DateTime { get; set; }
    }

    #endregion

    #region CreateBarCodeOrder

    public class CreateBarCodeOrder
    {
        /// <summary>
        /// Список заказов
        /// </summary>
        public List<CreatePrintFormOrder> Orders { get; set; }
        
        /// <summary>
        /// Число копий
        /// <remarks>По умолчанию 1</remarks>
        /// </summary>
        public int? CopyCount { get; set; }
        
        /// <summary>
        /// Формат печати.
        /// <remarks>Может принимать значения:
        /// <para>A4, A5, A6</para>
        /// <para>По умолчанию A4</para></remarks>
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Язык печатной формы
        /// <remarks>Возможные языки в кодировке ISO - 639-3:
        /// <para>Русский - RUS</para>
        /// <para>Английский - ENG</para></remarks>
        /// </summary>
        public string Lang { get; set; }
    }

    public class CreateBarCodeOrderResult
    {
        /// <summary>
        /// Информация о ШК месте к заказу
        /// </summary>
        public EntityUuid Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе над ШК местом к заказу
        /// </summary>
        public List<RequestInfo> Requests { get; set; }
    }

    #endregion

    #region GetBarCodeOrder

    public class GetBarCodeOrderResult
    {
        /// <summary>
        /// Информация о квитанции к заказу
        /// </summary>
        public EntityGetBarCodeOrder Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе над квитанцией к заказу
        /// </summary>
        public List<RequestInfo> Requests { get; set; }
    }

    public class EntityGetBarCodeOrder : EntityUuid
    {
        public List<CreatePrintFormOrder> Orders { get; set; }
            
        /// <summary>
        /// Число копий одной квитанции на листе
        /// </summary>
        public int? CopyCount { get; set; }
        
        /// <summary>
        /// Форма квитанции.
        /// </summary>
        public string Type { get; set; }
          
        /// <summary>
        /// Формат печати.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Язык печатной формы
        /// </summary>
        public string Lang { get; set; }
        
        /// <summary>
        /// Ссылка на скачивание файла
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Статус квитанции
        /// </summary>
        public List<FileStatus> Statuses { get; set; }
    }
    #endregion

    #region Intake

    public class IntakeParams
    {
        /// <summary>
        /// Номер заказа СДЭК
        /// </summary>
        public long? CdekNumber { get; set; }
        
        /// <summary>
        /// Идентификатор заказа в ИС СДЭК
        /// </summary>
        public Guid? OrderUuid { get; set; }
        
        /// <summary>
        /// Дата ожидания курьера
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter), "yyyy-MM-dd")]
        public DateTime IntakeDate { get; set; }
        
        /// <summary>
        /// Время начала ожидания курьера
        /// <remarks>Не ранее 9:00 местного времени</remarks>
        /// </summary>
        public string IntakeTimeFrom { get; set; }
        
        /// <summary>
        /// Время окончания ожидания курьера
        /// <remarks>Не позднее 22:00 местного времени</remarks>
        /// </summary>
        public string IntakeTimeTo { get; set; }
        
        /// <summary>
        /// Время начала обеда
        /// </summary>
        public string LunchTimeFrom { get; set; }
        
        /// <summary>
        /// Время окончания обеда
        /// </summary>
        public string LunchTimeTo { get; set; }
        
        /// <summary>
        /// Описание груза
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Общий вес (в граммах)
        /// </summary>
        public long Weight { get; set; }
        
        /// <summary>
        /// Длина (в сантиметрах)
        /// </summary>
        public int? Length { get; set; }
        
        /// <summary>
        /// Ширина (в сантиметрах)
        /// </summary>
        public int? Width { get; set; }
        
        /// <summary>
        /// Высота (в сантиметрах)
        /// </summary>
        public int? Height { get; set; }
        
        /// <summary>
        /// Комментарий к заявке для курьера
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// Отправитель
        /// </summary>
        public SenderIntake Sender { get; set; }
        
        /// <summary>
        /// Адрес отправителя (забора)
        /// </summary>
        public OrderLocation FromLocation { get; set; }
        
        /// <summary>
        /// Необходим прозвон отправителя
        /// <remarks>по умолчанию - false</remarks>
        /// </summary>
        public bool? NeedCall { get; set; }
    }

    public class SenderIntake
    {
        /// <summary>
        /// Название компании
        /// </summary>
        public string Company { get; set; }
        
        /// <summary>
        /// ФИО контактного лица
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Список телефонов
        /// <remarks>Не более 10 номеров</remarks>
        /// </summary>
        public List<Phone> Phones { get; set; }
    }
   
    public class IntakeResult
    {
        /// <summary>
        /// Информация о квитанции к заказу
        /// </summary>
        public EntityUuid Entity { get; set; }
        
        /// <summary>
        /// Информация о запросе над квитанцией к заказу
        /// </summary>
        public List<RequestInfo> Requests { get; set; }
    }
 
    #endregion

    #region Cities

    public class CitiesFilter
    {

        /// <summary>
        /// Массив кодов стран в формате  ISO_3166-1_alpha-2
        /// </summary>
        public List<string> CountryCodes { get; set; }

        /// <summary>
        /// Код региона по базе СДЭК
        /// </summary>
        public int? RegionCode { get; set; }

        public Guid? FiasGuid { get; set; }

        /// <summary>
        /// Почтовый индекс города
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Код населенного пункта СДЭК
        /// </summary>
        public int? Code { get; set; }

        /// <summary>
        /// Название населенного пункта. Должно соответствовать полностью
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Ограничение выборки результата.
        /// <remarks>По умолчанию 1000</remarks>
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// Локализация офиса.
        /// <para>По умолчанию "rus".</para>
        /// </summary>
        public string Lang { get; set; }
    }

    public class CityInfo
    {
        /// <summary>
        /// Код населенного пункта СДЭК
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Название населенного пункта.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Уникальный идентификатор ФИАС населенного пункта
        /// </summary>
        public Guid? FiasGuid { get; set; }

        /// <summary>
        /// Код страны населенного пункта в формате  ISO_3166-1_alpha-2
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Название страны населенного пункта
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Название региона населенного пункта
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Код региона СДЭК
        /// </summary>
        public int? RegionCode { get; set; }

        /// <summary>
        /// Название района региона населенного пункта
        /// </summary>
        public string SubRegion { get; set; }
        
        // /// <summary>
        // /// Массив почтовых индексов
        // /// </summary>
        // public List<string> PostalCodes { get; set; }
        
        /// <summary>
        /// Долгота центра населенного пункта
        /// </summary>
        public float? Longitude { get; set; }
        
        /// <summary>
        /// Широта центра населенного пункта
        /// </summary>
        public float? Latitude { get; set; }
        
        // /// <summary>
        // /// Часовой пояс населенного пункта
        // /// </summary>
        // public string TimeZone { get; set; }

        /// <summary>
        /// Ограничение на сумму наложенного платежа
        /// <remarks>-1 - ограничения нет;
        /// <para>0 - наложенный платеж не принимается;</para>
        /// <para>положительное значение - сумма наложенного платежа не более данного значения</para></remarks>
        /// </summary>
        public float? PaymentLimit { get; set; }
    }

    #endregion
}