using AdvantShop.Core.Common.Attributes;

namespace AdvantShop.Letters
{
    public enum OrderLetterTemplateKey
    {
        [LetterFormatKey("#ORDER_ID#", Description = "Идентификатор заказа")]
        OrderId,
        
        [LetterFormatKey("#ORDER_NUMBER#", Description = "Номер заказа")]
        Number,
        
        [LetterFormatKey("#EMAIL#", Description = "Email покупателя")] 
        Email,
        
        [LetterFormatKey("#FIRST_NAME#", Description = "Имя покупателя")]
        FirstName,
        
        [LetterFormatKey("#LAST_NAME#", Description = "Фамилия покупателя")]
        LastName,
        
        [LetterFormatKey("#FULL_NAME#", Description = "ФИО покупателя")]
        FullName,
        
        [LetterFormatKey("#PHONE#", Description = "Телефон покупателя")]
        Phone,
        
        [LetterFormatKey("#CITY#", Description = "Город покупателя")]
        City,
        
        [LetterFormatKey("#ADDRESS#", Description = "Адрес покупателя. Пример: Невский просп. д. 28 этаж 2")]
        Address,
        
        [LetterFormatKey("#ADDITIONAL_CUSTOMER_FIELDS#", Description = "Дополнительные поля покупателя. HTML")]
        AdditionalCustomerFields,
        
        [LetterFormatKey("#CUSTOMER_CONTACTS_HTML#", Description = "Информация о покупателе (ФИО, страна, регион, адрес). HTML")]
        CustomerContacts,
        
        [LetterFormatKey("#MANAGER_NAME#", Description = "Менеджер покупателя")]
        ManagerName,
        
        [LetterFormatKey("#MANAGER_SIGN#", Description = "Подпись менеджера покупателя")]
        ManagerSign,
        
        [LetterFormatKey("#INN#", Description = "ИНН (Счет на оплату)")]
        Inn,

        [LetterFormatKey("#COMPANY_NAME#", Description = "Название компании (Счет на оплату)")]
        CompanyName,
        
        [LetterFormatKey("#KPP#", Description = "КПП (Счет на оплату)")]
        Kpp,


        [LetterFormatKey("#ORDER_STATUS#", Description = "Статус заказа")]
        Status,
        
        [LetterFormatKey("#ORDER_STATUS_COMMENT#", Description = "Комментарий к статусу заказа")]
        StatusComment,
        
        [LetterFormatKey("#ORDER_STATUS_COMMENT_HTML#", Description = "Комментарий к статусу заказа. HTML")]
        StatusCommentHtml,
        
        
        [LetterFormatKey("#SHIPPING_NAME_FULL#", Description = "Название доставки и пункт выдачи. HTML")]
        ShippingNameWithPickpointAddressHtml,
        
        [LetterFormatKey("#SHIPPING_NAME#", Description = "Название доставки")]
        ShippingName,
        
        [LetterFormatKey("#ORDER_PICKPOINT_ADDRESS#", Description = "Пункт выдачи")]
        PickpointAddress,
        
        [LetterFormatKey("#PAYMENT_NAME#", Description = "Название оплаты")]
        PaymentName,
        
        [LetterFormatKey("#ORDER_PAY_STATUS#", "#PAY_STATUS#", Description = "Статус оплаты: \"произведена\", \"отменена\"")]
        PaymentStatus,
        
        [LetterFormatKey("#ORDER_IS_PAID#", Description = "Статус оплаты: \"оплачен\", \"не оплачен\"")]
        OrderPaidOrNotPaid,
        
        [LetterFormatKey("#BILLING_SHORT_LINK#", Description = "Короткая ссылка на оплату")]
        BillingShortLink,
        
        [LetterFormatKey("#BILLING_LINK#", Description = "Cсылка на оплату")]
        BillingLink,
        
        [LetterFormatKey("#ORDER_ITEMS_HTML#", Description = "Товары заказа. HTML")]
        OrderItemsHtml,
        
        [LetterFormatKey("#ORDER_ITEMS_PLAIN#", Description = "Товары заказа без ссылок на карточки товара. HTML")]
        OrderItemsPlain,
        
        [LetterFormatKey("#ORDER_ITEMS_HTML_DOWNLOAD_LINKS#", Description = "Товары заказа с ссылками на скачивание. HTML")]
        OrderItemsHtmlDownloadLinks,
        
        [LetterFormatKey("#ORDER_ITEMS_PLAIN_DOWNLOAD_LINKS#", Description = "Товары заказа без ссылок на карточки товара с ссылками на скачивание. HTML")]
        OrderItemsPlainDownloadLinks,
        
        [LetterFormatKey("#SUM#", Description = "Сумма заказа. Пример: 1 100 руб.")]
        Sum,
        
        [LetterFormatKey("#SUM_WITHOUT_CURRENCY#", Description = "Сумма заказа. Пример: 1 100")]
        SumWithoutCurrency,
        
        [LetterFormatKey("#SHIPPING_COST#", Description = "Стоимость доставки. Пример: 1 100 руб.")]
        ShippingCost,
        
        [LetterFormatKey("#SHIPPING_COST_WITHOUT_CURRENCY#", Description = "Стоимость доставки. Пример: 1 100")]
        ShippingCostWithoutCurrency,
        
        [LetterFormatKey("#ORDER_TRACK_NUMBER#", Description = "Номер отслеживания (трек-номер)")]
        TrackNumber,

        [LetterFormatKey("#ORDER_CURRENCY_CODE#", Description = "Код валюты: RUB, USD")]
        CurrencyCode,
        
        [LetterFormatKey("#ORDER_CUSTOMER_COMMENT#", Description = "Комментарий покупателя к заказу")]
        CustomerComment,

        [LetterFormatKey("#DELIVERYDATE#", Description = "Дата доставки. Пример: \"Дата доставки: 7.10 09:00-11:00\"")]
        DeliveryDateWithPrefix,
        
        [LetterFormatKey("#DELIVERYDATE_DATE#", Description = "Дата доставки. Пример: \"7.10 09:00-11:00\"")]
        DeliveryDate,

        [LetterFormatKey("#POSTAL_CODE#", Description = "Индекс")]
        PostalCode,

        [LetterFormatKey("#RECEIVING_METHOD#", Description = "Способ получения заказа")]
        ReceivingMethod,

        [LetterFormatKey("#COUNT_DEVICES#", Description = "Количество приборов")]
        CountDevices,
        
        [LetterFormatKey("#RECIPIENT_LAST_NAME#", Description = "Фамилия получатель заказа")]
        RecipientLastName,
        
        [LetterFormatKey("#RECIPIENT_FIRST_NAME#", Description = "Имя получатель заказа")]
        RecipientFirstName,

        [LetterFormatKey("#RECIPIENT_PHONE#", Description = "Номер телефона получатель заказа")]
        RecipientPhone,

        [LetterFormatKey("#GENERATED_COUPON_CODE#", Description = "GENERATED_COUPON_CODE")]
        GeneratedCouponCode,//GlorySoft_003
    }
}