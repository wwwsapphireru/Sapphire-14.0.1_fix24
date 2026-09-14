using AdvantShop.Catalog;
using AdvantShop.Core;
using AdvantShop.Core.Common.Extensions;
using AdvantShop.Core.Modules;
using AdvantShop.Core.Services.Configuration.Settings;
using AdvantShop.Core.Services.Triggers;
using AdvantShop.Customers;
using AdvantShop.Diagnostics;
using AdvantShop.FilePath;
using AdvantShop.Helpers;
using AdvantShop.Module.OneSApi.Domain;
using AdvantShop.Module.OneSApi.Handlers.Admin;
using AdvantShop.Module.OneSApi.Handlers.Api.Customers;
using AdvantShop.Module.OneSApi.Handlers.Api.Orders;
using AdvantShop.Module.OneSApi.Handlers.Api.Product;
using AdvantShop.Module.OneSApi.Handlers.Api.Products;
using AdvantShop.Module.OneSApi.Models.Admin;
using AdvantShop.Module.OneSApi.Models.Api;
using AdvantShop.Module.OneSApi.Service;
using AdvantShop.Web.Infrastructure.Admin;
using AdvantShop.Web.Infrastructure.Controllers;
using AdvantShop.Web.Infrastructure.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AdvantShop.Module.OneSApi.Controllers
{
    public class OneSAdminController : ModuleAdminController
    {

        #region Views

        [ChildActionOnly]
        public ActionResult Settings()
        {
            //            var s = @"84959958488@bk.ru;Авакова Юлия Вячеславовна
            //avetyan.2024@inbox.ru;Аветян Нарек Георгиевич
            //comden@gmail.com;Агапов Денис Федорович
            //sasha.levickiy@mail.ru;Агафонова Евгения Павловна
            //annaa-1693@mail.ru;Александрова Анна Сергеевна
            //self@list.ru;Алексеев Вячеслав Эдуардович
            //avtolux@mail.ru;Алексеев Кирилл Владимирович
            //a.lexus@inbox.ru;Алексеев Сергей Викторович
            //alangrey@yandex.ru;Алексеев Сергей Сергеевич
            //kitika_k@mail.ru;Алябьева Юлия Владимировна
            //aao399@mail.ru;Аникина Алёна Олеговна
            //aniskin-vm@yandex.ru;Анискин Владимир Михайлович
            //antipov-timofei@mail.ru;Антипов Тимофей Михайлович
            //articpenguin45rus@gmail.com;Ануфриев Даниил Викторович
            //april180@gmail.com;Артеменко Мария Владимировна
            //support57@mail.ru;Арутюнян Геннадий Суренович
            //8347-1969@inbox.ru;Ахметгалиев Рудольф Ринатович
            //s.a.babanin@gmail.com;Бабанин Сергей Алексеевич
            //Avadiseme@gmail.com;Бабашинская Алёна Максимовна
            //Nomad.01@mail.ru;Бабушкин Александр Николаевич
            //tsbalakina@gmail.com;Балакина Татьяна Сергеевна
            //Kim093@mail.ru;Балахнина Валентина Леонидовна
            //balinilya@gmail.com;Балин Илья Валентинович
            //Nymka@yandex.ru;Баранова Дарья Юрьевна
            //bautina92@bk.ru;Баутина Вероника Алекссевна
            //helenbahur@gmail.com;Бачурина Елена Евгеньевна
            //innusik@yandex.ru;Белоусова Инна Сергеевна
            //robocop77754@gmail.com;Бирюков Александр Владимирович
            //korolewa1785@mail.ru;Богданова Наталия Викторовна
            //nozdriosla@gmail.com;Богдасарова Ксения Олеговна
            //k111xn@yandex.ru;Богус Юлия Борисовна
            //bodnar.vasilina2014@yandex.ru;Боднарь Василина Владимировна
            //mozgolomchik@yandex.ru;Бойко Максим Михайлович
            //mvborisova79@mail.ru;Борисова Марина Владимировна
            //bv55@yandex.ru;Братчиков Владимир Николаевич
            //broderbrod@gmail.com;Бродский Илья Александрович
            //bukatkina@mail.ru;Букаткина Анна Константиновна
            //apbukov@gmail.com;Буков Алексей Петрович
            //maksbulygin@gmail.com;Булыгин Булыгин Максим Юрьевич
            //alexandrabur2@gmail.com;Буренкова Александра Сергеевна
            //vasilij.buriak@gmail.com;Бурьяк Василий Вячеславович
            //butenkoff@mail.ru;Бутенков Евгений Андреевич
            //lex2233@gmail.com;Бушмакин Роман Витальевич
            //imaionez13@ya.ru;Вагин Артем Александрович
            //valyaev.igor@icloud.com;Валяев Игорь Сергеевич
            //LediLoki@yandex.ru;Василькова Ольга Дмитриевна
            //annnnav48@gmail.com;Васькова Анна Анатольевна
            //makikimora@gmail.com;Вахрамеев Михаил Владимирович
            //vahtangov1921@mail.ru;Вахтангов Дмитрий Николаевич
            //jeweler-vva@mail.ru;Веретенников Виталий Александрович
            //hellstar@rambler.ru;Верижников Дмитрий Викторович
            //4ykotka89@inbox.ru;Войцицкий Андрей Анатольевич
            //rainbow_forest@mail.ru;Волкова Елена Геннадьевна
            //av777-08@mail.ru;Воробьев Андрей Николаевич
            //vladimir.gaverd1955@gmail.com;Гавердовский Владимир Александрович
            //Drema1997@list.ru;Гаврилов Артем Сергеевич
            //Radislav-gaiduk@yandex.ru;Гайдук Радислав Андреевич
            //galievaag@mail.ru;Галиева Алина Галиевна
            //Galkin1987@yandex.ru;Галкин Александр Львович
            //galkin1475@mail.ru;Галкин Вадим Николаевич
            //ali13zakup@mail.ru;Гандзюк Алина Олеговна
            //note.name@rambler.ru;Гарин Александр Алексеевич
            //emil_arsenal@mail.ru;Гаспарян Меружан/Маргарян Эмиль Каренович
            //skg8@yandex.ru;Гвоздев Сергей Кимович
            //vadikpsk@mail.ru;Геращенко Вадим Александрович
            //LadyLifies@gmail.com;Голубева Евгения Михайловна
            //fragmentart1@gmail.com;Городяненко Андрей Викторович
            //Mix-Advert@yandex.ru;Горохова Диана Дмитриевна
            //a.goryaeva1@mail.ru;Горяева Алина Арсеньевна
            //avg.15@mail.ru;Горячев Андрей Владимирович
            //inna.grabar@mail.ru;Грабар Инна Леонидовна
            //rnsc.skfo@gmail.com;Гребенюк Андрей Александрович
            //antongrudin@gmail.com;Грудин Антон Валерьевич
            //only_gucci@mail.ru;Гукова Мария Вениаминовна
            //kurg-i@mail.ru;Гущин Игорь Валентинович
            //maxood.d@yandex.ru;Давиденко Максим Владимирович
            //Davshan.ilya@mail.ru;Давшан Илья Витальевич
            //david-d75@bk.ru;Данилов Давид Борисович
            //o_danilova@yahoo.com;Данилова Ольга Владимировна
            //deli2009@yandex.ru;Дельвиг Александра Алексеевна
            //celterium@gmail.com;Дельнов Сергей Владимирович
            //demidenko-irina.1996@yandex.ru;Демиденко Ирина Дмитриевна
            //kuzinahaos@yandex.ru;Деопик Ольга Дмитриевна
            //margodergunova96@gmail.com;Дергунова Маргарита Олеговна
            //dzhony14@mail.ru;Дмитриев Евгений Дмитриевич
            //batrovaolya@mail.ru;Добрынина Ольга Александровна
            //tatiana1.muffet.01@mail.ru;Довбня Татьяна Игоревна
            //pionerr64@mail.ru;Доронкин Александр Геннадьевич
            //dorofeevvn@1064c.cc;Дорофеев Василий Николаевич
            //taiga178@rambler.ru;Волкова Татьяна Николаевна
            //Gd@profvo.ru;Дрожжин Глеб Александрович 
            //de02@mail.ru;Дуванова Екатерина Александровна
            //consc@ya.ru;Дурнев Константин Алексеевич
            //aveshadow@yandex.ru;Евтеев Алексей Владимирович
            //yemengulov@yandex.ru;Еменгулов Юрий Николаевич
            //zhara@mail.ru;Жарков Cергей Владимирович
            //zhvirblis.andrei@yandex.ru;Жвирблис Андрей Юрьевич
            //graniostroty@gmail.com;Желтоухов Демид Андреевич
            //docentzh@mail.ru;Жуков Александр Михайлович
            //ya6or@yandex.ru;Журавлев Борис Дмитриевич
            //arhitrav@mail.ru;Засимова Олеся Валерьевна
            //BobBobov161@yandex.ru;Захаревич Даниил Владиславович
            //Artyr9719@yandex.ru;Захаров Артур Дмитриевич
            //masharais0796@gmail.com;Захарова Мария Дмитриевна
            //mihailzahezin@gmail.com;Захезин Михаил Владимирович
            //Zav-ultra@yandex.ru;Зворыгин Александр Владимирович
            //nanotex055@gmail.com;Зелепукин Юрий Анатольевич
            //nikitos.geo@yandex.ru;Зенкин Никита Николаевич
            //zinovkin.ol@yandex.ru;Зиновкин Олег Владимирович
            //evgeniiazolotareva@mail.ru;Золотарева Евгения Дмитриевна
            //ivanov19@list.ru;Иванов Анатолий Геннадьевич
            //i@zxqnn.ru;Иванов Святослав Александрович
            //shogunsan@mail.ru;Иванов Сергей Геннадьевич
            //info@marcel-robert.com;Иванцова Юлия Юрьевна
            //Aivmajor@gmail.com;Иванцова Юлия/Иванцова Анна
            //ya.aleksandra-0@yandex.ru;Игнатенко Александра Александровна
            //alexandr_d.i@mail.ru;Игнатов Александр Денисович
            //nondriveside@gmail.com;Игольников Александр Сергеевич
            //nazir7171@mail.ru;Идрисов Назир Бирцинаевич
            //atismanov@gmail.com;Исманов Артур Талантович
            //mr.zet147@gmail.com;Каверин Сергей Владимирович
            //Bum1er@yandex.ru;Казаков Артур Георгиевич
            //usb2006@rambler.ru;Кайданович Дмитрий Анатольевич
            //Siroga26ru@ya.ru;Камышов Сергей Александрович
            //apollo_d@mail.ru;Карлин Денис Сергеевич
            //kdensb@yandex.ru;Карпов Денис Борисович
            //amant-020@yandex.ru;Карташов Николай Николаевич
            //kazcura@bk.ru;Керимова Тина Камильевна
            //wadimart@gmail.com;Кириллов Вадим Игоревич
            //mkirilchuk@yandex.ru;Кирильчук Максим Григорьевич
            //igjir2009@yandex.ru;Киселев Игорь Викторович
            //dmitri-ki@mail.ru;Киселевский Дмитрий Викторович
            //irkisel@gmail.com;Кисель Ирина Геннадьевна
            //gertruda2002@mail.ru;Клапцова Ольга Витальевна
            //Klevcov89@mail.ru;Клевцов Александр Викторович
            //12nastyia@gmail.com;Клочкова Анастасия Денисовна
            //kovalev02091985@gmail.com;Ковалев Евгений Викторович
            //ilia.kovalev81@gmail.com;Ковалев Илья Андреевич
            //4442344@bk.ru;Комур Виктор Петрович
            //smart-fresh@yandex.ru;Коник Александр Васильевич
            //lartdoll@gmail.com;Константинова Лада Юрьевна
            //ztlk2005@mail.ru;Копылов Матвей Александрович
            //s-kormin@mail.ru;Кормин Сергей Владимирович
            //kornipaevak@mail.ru;Корнипаева Христина Ильинична
            //korobtsov-alex@mail.ru;Коробцов Александр Анатольевич
            //gallery@icon-stavros.ru;Косенков Сергей Егорович
            //vladkr4@yandex.ru;Кравченко Владислав Валерьевич
            //krasavinigor@bk.ru;Красавин Игорь Александрович
            //10052005l@mail.ru;Кривопускова Елена Витальевна
            //alex19.99black@mail.ru;Кротов Алексей Владимирович
            //2336355@mail.ru;Крюков Илья Валерьевич
            //kse-sivakova@yandex.ru;Ксения Сивакова Андреевна
            //kudaibergenov.r@mail.ru;Кудайбергенов Рамазан Русланович
            //evgeniy.kuznetsov.63@mail.ru;Кузнецов Евгений Леонидович
            //kcobmen@mail.ru;Кузнецов Сергей Эдуардович
            //na-na2004@yandex.ru;Кузнецова Анастасия Руслановна
            //fern.bloss.om@yandex.ru;Кулаева Анна Николаевна
            //kke001@yandex.ru;Курьянович Константин Евгеньевич
            //owl-lee@yandex.ru;Лащук Елена Олеговна
            //yulia.ananieva@gmail.com;Лебедева Юлия Евгеньевна
            //yartzew@gmail.com;Левченко Алексей Анатольевич
            //liselsh@gmail.com;Лисицын Александр Александрович
            //foxbobox@gmail.com;Лисицын Борис Игоревич
            //lissfog@yandex.ru;Лисянский Роман Валерьевич
            //89133833004@mail.ru;Литвиненко Павел Дмитриевич
            //zhen-litvin@yandex.ru;Литвинов Евгений Александрович
            //litus_sv@mail.ru;Литус Станислав Валериевич
            //ktp2009@mail.ru;Личидов Станислав Вячеславович
            //zremeslo585@mail.ru;Лодыгина Юлия Сергеевна
            //yura_l76@mail.ru;Ломов Юрий Викторович
            //Irinalosk@mail.ru;Лоскутникова Ирина Анатольевна
            //sigorila@yandex.ru;Лукичев Игорь Сергеевич
            //a112358132134@ya.ru;Лукьянов Алексей Анатольевич
            //lukyanova-anna94@mail.ru;Лукьянова Анна Ярославна
            //zakupki@denta-plus.org;Майсейшина Екатерина Геннадьевна
            //katemoza@mail.ru;Малород Екатерина Николаевна
            //ie19@ya.ru;Маркина Ирина Владимировна
            //masloff63@mail.ru;Маслов Александр Юрьевич
            //maslonastya@yandex.ru;Маслова Анастасия Дмитриевна
            //ms_agata@mail.ru;Мащенко Светлана Анатольевна
            //artem.milekhin@yandex.ru;Милёхин Артемий Михайлович
            //just_art@bk.ru;Митлевская Ирина Сергеевна
            //chy0rnyy@mail.ru;Митюгов Сергей Владимирович
            //mminvestgroup@mail.ri;Михайлов Владимир Николаевич
            //Mishkinew@yandex.ru;Мишкин Евгений Владимирович
            //sorreldog@gmail.com;Мищенко Ирина Юрьевна
            //aandmorozov@gmail.com;Морозов Андрей Андреевич
            //mns.morozova@mail.ru;Морозова Наталия Сергеевна
            //ASHKA--74@yandex.ru;Мурадян Ашот Геворгевич
            //muranna19@gmail.com;Муромцева Анна Валерьевна
            //Prosha6634@gmail.com;Муртаев Вячеслав Алишерович
            //Travers2004@mail.ru;Мусатов Антон Вячеславович
            //louiseattaque@mail.ru;Мусостова Луиза Камалдиновна
            //lena.myasnikova.104@mail.ru;Мясникова Елена Николаевна
            //ericcartman075@gmail.com;Назаров Андрей Константинович
            //Arm7771974@mail.ru;Назарян Арсен Адраникович
            //M@NALEVIN.RU;Налевин Михаил Викторович
            //gabenbal@mail.ru;Науменко Елена Владимировна
            //av.netrebskaya@gmail.com;Нетребская Анна Владимировна
            //vitaly.www@gmail.com;Никитин Виталий Геннадьевич
            //alex.nipevgi@mail.ru;Ныпевги Александра Валерьевна
            //Oborotova.o.v@gmail.com;Оборотов Виктор Владимирович
            //mussuc@yandex.ru;Овод Таисия Евгеньевна
            //restfar@yandex.ru;Олейникова Ольга Игоревна
            //olhowa.darya@gmail.com;Ольхова Дарья Алексеевна
            //amor-tizator@mail.ru;Охотников Илья Игоревич
            //dimetrijx@gmail.com;Павлов Дмитрий Сергеевич
            //pavlovadara383@gmail.com;Павлова Анна Сергеевна
            //natikp7@rambler.ru;Павлова Наталья Александровна
            //Padalkoak@gmail.com;Падалко Анастасия Кирилловна
            //sikander@list.ru;Панаскин Александр Владимирович
            //panichevd563@gmail.com;Паничев Даниил Романович
            //aaa.aiphon@gmail.com;Панфиленкова Елена Геннадьевна
            //vladikboxx@mail.ru;Пашолок Владислав Игоревич
            //Jyliy_1978@mail.ru;Перфилова Юлия Владимировна
            //glebpetrov718@gmail.com;Петров Глеб Петрович
            //depetrov2303@yandex.ru;Петров Денис Вадимович
            //a-piv@yandex.ru;Пивкин Андрей Петрович
            //filpiskun@yandex.ru;Пискун Филипп Эдуардович
            //dimanordimon@mail.ru;Пладес Дмитрий Александрович
            //poddubny.dmitry@gmail.com;Поддубный Дмитрий Владимирович
            //timurpodzorov@yandex.ru;Подзоров Тимур Викторович
            //polukhin.info@yandex.ru;Полухин Егор Владимирович
            //mail@dpopov-spb.ru;Попов Дмитрий Андреевич
            //masyna@bk.ru;Попова Надежда Алексеевна
            //polinka96@mail.ru;Попова Полина Олеговна
            //sapphire@j-tull.ru;Пташник Кирилл Витальевич
            //_teapot@mail.ru;Путрин Андрей Валерьевич
            //dcklab@yandex.ru;Пятков Евгений Николаевич
            //Cherry1979@bk.ru;Рабинович Юлия Сергеевна
            //ya.rakhmanova@gmail.com;Рахманова Ярослава Николаевна
            //cgmaxim@gmail.com;Романюк Максим Владимирович
            //ru.85@list.ru;Румянцев Александр Анатольевич
            //rumss84@mail.ru;Румянцев Сергей Сергеевич
            //corvusrus@yandex.ru;Русанов Юрий Александрович
            //maric2021@mail.ru;Рыжков Юрий Васильевич
            //gibs_on@mail.ru;Рябцев Олег Сергеевич
            //lsr.giro@gmail.com;Сабиров Ленар Робертович
            //vidogo@yandex.ru;Савельев Михаил Александрович
            //saliji93@mail.ru;Салий Илья Федорович
            //yurasamsonov89@mail.ru;Самсонов Юрий Юрьевич
            //zaza231070@mail.ru;Саргсян Овсеп Зарзандович
            //anjasvetashva@yandex.ru;Светашова Анна Алексеевна
            //natasvetlaya15@gmail.com;Светлова Наталья Владимировна
            //sooner-or-later@yandex.ru;Селукова Вера Александровна
            //infau@yandex.ru;Сергеева Людмила Борисовна
            //astalavista29@mail.ru;Силаева Майя Сергеевна
            //insolence_e@mail.ru;Скворцова Татьяна Николаевна
            //sergey57@bk.ru;Скрягин Сергей Николаевич
            //smirnov@helicon.ru;Смирнов Анатолий Сергеевич
            //smrnvd@gmail.com;Смирнов Даниил Георгиевич
            //mashkakapitoshka@mail.ru;Смирнова Мария Викторовна
            //soniasmirn@gmail.com;Смирнова Софья Алексеевна
            //9154292289@mail.ru;Собаченков Алексей Дмитриевич
            //svv31176@yandex.ru;Соболев Валерий Владимирович
            //osok65@yandex.ru;Соколова Ольга Валериевна
            //sma_sh@mail.ru;Солдатов Александр Сергеевич
            //ivan.sax.pro@gmail.com;Солдатов Иван Андреевич
            //soloviev56@bk.ru;Соловьев Андрей Алексеевич
            //sotnik.720@mail.ru;Сотник Виталий Георгиевич
            //mr-mraks@yandex.ru;Сошкин Максим Александрович
            //3dwaxmaster@mail.ru;Стальцев Владимир Александрович
            //1461516@mail.ru;Стародумова Евгения Сергеевна
            //stekaleksandr@yandex.ru;Стеклов Александр Владимирович
            //cobrini@rambler.ru;Стеклов Александр Михайлович
            //oct@inbox.ru;Стекольщиков Олег Юрьевич
            //andreistrunin@mail.ru;Струнин Андрей Михайлович
            //7772120@mail.ru;Султанян Аршалуйс Сеирянович
            //cupids-delight@yandex.ru;Суслонова Анна Александровна
            //mmm_sr_tlt@yahoo.com;Сюбаев Рафаэль Шакирович
            //elvira_22.90@mail.ru;Тагиева Эльвира Эльдаровна
            //igr-t@list.ru;Талаев Игорь Сергеевич
            //lego2409@mail.ru;Тарасов Олег Александрович
            //tda6@rambler.ru;Темников Дмитрий Александрович
            //anna.lea@yandex.ru;Терских Анна Павловна
            //Jinned@yandex.ru;Титов Николай Николаевич
            //likratowa@gmail.com;Токмакова Елена Александровна
            //tolstickov.lesha@gmail.com;Толстиков Алексей Александрович
            //nataliayunusova4@gmail.com;Тонкаль Наталия Валерьевна
            //Aleksandr714@rambler.ru;Трофименко Александр Александрович
            //yellowmazestudio@gmail.com;Трунов Алексей Анатольевич
            //eevgttul@gmail.com;Тулянкина Евгения Максимовна
            //Tuychkalov_ad@mail.ru;Тючкалов Алексей Дмитриевич
            //nadya-u@rambler.ru;Уварова Надежда Фидаильевна
            //alenaviktorovna@gmail.com;Ульянихина Алена Викторовна
            //super.aiika2015@yandex.ru;Фальковский Иван Васильевич
            //sf@restotouch.ru;Федоров Сергей Николаевич
            //sergofan121284@gmail.com;Федяев Сергей Сергеевич
            //shop@proof.ru;Филатов Антон Евгеньевич
            //margarita_filimonova@list.ru;Филимонова Маргарита Алексеевна
            //der4@yandex.ru;Фокин Кирилл Анатольевич
            //fsilen@yandex.ru;Фролов Сергей Александрович
            //mw878@rambler.ru;Ханова Екатерина Сергеевна
            //Ketsune161@gmail.com;Хрячков Кирилл Александрович
            //alexandra_ts@mail.ru;Царькова Александра Сергеевна
            //charinaekaterina@gmail.com;Чарина Екатерина Александровна
            //pavelcerepanov75554@gmail.com;Черепанов Павел Николаевич
            //viktor.chernega.74@mail.ru;Чернега Виктор Александрович
            //olegtoyota@mail.ru;Чуликанов Олег Вячеславович
            //mail0926000@gmail.com;Чумаков Евгений Алексеевич
            //factorialzero@gmail.com;Чучелов Дмитрий Сергеевич
            //sm414@yandex.ru;Шапран Андрей Рамильевич
            //Chassagnard.maxyan@gmail.com;Шассаньяр Максян Жильбертович
            //elen1971@yandex.ru;Шибаева Елена Викторовна
            //Iris-mag@yandex.ru;Шипилова Ирина Игоревна
            //cworks@mail.ru;Ширяев Константин Владимирович
            //ok-ff@list.ru;Шляндин Михаил Леонидович
            //ingyang77@mail.ru;Шмурыгина Ольга Михайловна
            //perekatt@mail.ru;Шпак Оксана Геннадиевна
            //shuvatovroman@gmail.com;Шуватов Роман Александрович
            //auroom63@mail.ru;Шульга Вадим Витальевич
            //egnatosyan82@mail.ru;Эгнатосян Васили Меружанович
            //Lurea1@mail.ru;Юря Роман Николаевич
            //kl29995@mail.ru;Явкина Олеся Витальевна
            //yakovlevserg27@gmail.com;Яковлев Сергей Витальевич
            //mary310391@mail.ru;Яковлева Мария Витальевна
            //yakovleva.uliana@gmail.com;Яковлева Ульяна Павловна
            //god003@rambler.ru;Якубов Год Ченгизовеч
            //hej@waldstation.ru;Янссон Дарья Николаевна
            //marieyastrebova@icloud.com;Ястребова Мария Дмитриевна
            //";
            //            var a = s.Split("\r\n");
            //            foreach (var item in a)
            //            {
            //                var str = item.Split(';');
            //                var c = CustomerService.GetCustomerByEmail(str[0]);
            //                if (c != null)
            //                {
            //                    var fio = str[1].Split(" ");
            //                    if (fio.Length > 2 && fio[2].IsNotEmpty())
            //                        c.Patronymic = fio[2].Trim();
            //                    if (fio.Length > 1 && fio[1].IsNotEmpty())
            //                        c.FirstName = fio[1].Trim();
            //                    if (fio.Length > 0 && fio[0].IsNotEmpty())
            //                        c.LastName = fio[0].Trim();
            //                    CustomerService.UpdateCustomer(c);
            //                }
            //            }

            //var expired = ModuleService.GetExpiredConfirmRegistrartion();
            //Debug.Log.Info(JsonConvert.SerializeObject(expired));

            //ModuleService.UpdateModule();////

            //var img = FoldersHelper.GetImageProductPath(ProductImageType.XSmall, "16458.jpeg", false);
            //var url = AdvantShop.Core.UrlRewriter.UrlService.GenerateBaseUrl();
            //System.Web.HttpContext.Current.Items["BaseUrl"] = AdvantShop.Configuration.SettingsMain.SiteUrl;

            //var path = FoldersHelper.GetPathAbsolut(FolderType.ImageTemp, "photoFull_DELETE_list.csv");
            //using (var reader = new CsvHelper.CsvReader(new StreamReader(path, System.Text.Encoding.GetEncoding(ExportImport.EncodingsEnum.Utf8.StrName())),
            //        new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            //        {
            //            Delimiter = ExportImport.SeparatorsEnum.SemicolonSeparated.StrName(),
            //            HasHeaderRecord = true
            //        }))
            //{
            //    reader.Read();
            //    reader.ReadHeader();
            //    while (reader.Read())
            //    {
            //        if (reader[1].ToString().IsNullOrEmpty())
            //            continue;
            //        var artno = reader[0].ToString().Replace("SKU_", "");
            //        var p = ProductService.GetProduct(artno);
            //        if (p == null)
            //            continue;
            //        var delete = reader[1].ToString().Split(',').Select(x => x.Trim()).ToList();
            //        foreach (var del in delete)
            //        {
            //            var d = del.Split("_");
            //            var ph = p.ProductPhotos.SingleOrDefault(x => x.PhotoId == d[0].TryParseInt());
            //            if (ph != null)
            //                PhotoService.DeleteProductPhoto(ph.PhotoId);
            //        }
            //    }
            //}

            //var path = FoldersHelper.GetPathAbsolut(FolderType.ImageTemp, "photos");
            //var ids = ProductService.GetAllProductIDs();
            //foreach (var id in ids)
            //{
            //    var p = ProductService.GetProduct(id);
            //    if (!p.CategoryEnabled)
            //        continue;
            //    if (p.ProductPhotos.Count <= 1)
            //        continue;
            //    var phPath = path.TrimEnd('\\').TrimEnd('/') + $"\\SKU_{p.ArtNo}";
            //    var b = true;
            //    foreach (var ph in p.ProductPhotos)
            //    {
            //        //Debug.Log.Warn(ph.PhotoName);
            //        string tmpFilePath;
            //        var image = ProductServiceV8.ImageFromFile(path, ph.PhotoId.ToString(), out tmpFilePath);
            //        if (image != null)
            //        {
            //            if (image.Width >= 1000 && image.Height >= 1000)
            //                b = false;
            //        }
            //    }
            //    if (!b) continue;
            //    FileHelpers.CreateDirectory(phPath);
            //    foreach (var ph in p.ProductPhotos)
            //    {
            //        //Debug.Log.Warn(ph.PhotoName);
            //        var photoNames = ph.PhotoName.Split('/');
            //        var photoName = photoNames[photoNames.Length - 1].Split('.');
            //        var file = $"{photoName[0]}_big.{photoName[1]}";
            //        //var file = $"{photoName[0]}.{photoName[1]}";////
            //        var big = FoldersHelper.GetPathAbsolut(FolderType.Product, "big").TrimEnd('\\').TrimEnd('/') + $"\\{file}";
            //        try
            //        {
            //            System.IO.File.Copy(big, phPath + $"\\{file}");
            //        }
            //        catch (Exception e)
            //        {
            //            Debug.Log.Warn(e);
            //            Debug.Log.Warn(big);
            //            Debug.Log.Warn(phPath + $"\\{file}");
            //        }
            //    }
            //    //break;
            //}

            //var result = "";
            //var products = ProductService.GetAllProductsByIds(ProductService.GetAllProductIDs()).Where(x => x.Enabled).ToList();
            //foreach (var p in products)
            //{
            //    var res = $"{p.ArtNo};https://www.sapphire.ru/adminv3/product/edit/{p.ProductId}#photos";
            //    var photos = PhotoService.GetPhotos(p.ProductId, PhotoType.Product).ToList();
            //    if (photos.Count == 0)
            //    {
            //        result += res;
            //        result += "\r\n";
            //    }
            //    //var t = 0;
            //    var f = 0;
            //    foreach (var ph in photos)
            //    {
            //        //var path = p.PhotoBig.Replace("http://localhost:8830", "https://sapphire.ru");
            //        var path = $"https://www.sapphire.ru/pictures/product/big/{ph.PhotoId}_big.jpeg";
            //        string tmpFilePath;
            //        var image = ProductServiceV8.ImageFromFile(path, ph.PhotoId.ToString(), out tmpFilePath);
            //        if (image != null)
            //        {
            //            res += $";{image?.Width}x{image?.Height}";
            //            if (image.Width <= 250 || image.Height <= 250)
            //                f++;
            //        }
            //        //else
            //        //{
            //        //    res += ";0x0";
            //        //    f += 2;
            //        //}
            //    }
            //    if (f >= 2)
            //    {
            //        result += res;
            //        result += "\r\n";
            //    }
            //    //break;
            //}
            //var csvFilePath = FoldersHelper.GetPathAbsolut(FolderType.ImageTemp, "photos.csv");
            //System.IO.File.WriteAllText(csvFilePath, result);

            //var customers = CustomerService.GetCustomers();
            //foreach (var c in customers)
            //{
            //    var s = SubscriptionService.GetSubscription(c.EMail);
            //    var v = c.IsAgreeForPromotionalNewsletter;
            //    if ((s?.Subscribe ?? false) != v)
            //    {
            //        if (v)
            //            SubscriptionService.Subscribe(c.EMail);
            //        else
            //            SubscriptionService.Unsubscribe(c.EMail);
            //    }
            //}

            //string ip = Request.UserHostAddress;
            //string apiKey = "ВАШ_API_КЛЮЧ";
            //string url = $"https://ipqualityscore.com/api/json/ip/{apiKey}/{ip}";
            //WebClient client = new WebClient();
            //string json = client.DownloadString(url);
            //dynamic data = JObject.Parse(json);
            //if (data.vpn == true)
            //{
            //    vpnWarning.Visible = true;
            //}

            return PartialView("~/modules/" + OneSApi.ModuleStringId + "/Views/Admin/Settings.cshtml");
        }

        [ChildActionOnly]
        public ActionResult Export()
        {
            return PartialView("~/modules/" + OneSApi.ModuleStringId + "/Views/Admin/Export.cshtml");
        }

        [ChildActionOnly]
        public ActionResult Logs()
        {
            return PartialView("~/modules/" + OneSApi.ModuleStringId + "/Views/Admin/Logs.cshtml");
        }

        #endregion

        #region Settings

        [HttpGet]
        public JsonResult GetSettings()
        {
            var settings = ModuleService.GetImportExportSettings();

            var tmp1 = CategoryService.GetChildCategoriesByCategoryId(0, false).ToList();
            tmp1.Insert(0, new Category { CategoryId = 0, Name = "Каталог" });
            var defaultCategoryList = tmp1.Select(x => new
            {
                label = x.Name,
                value = x.CategoryId.ToString(),
            });

            var importArtnoTypeList = Enum.GetValues(typeof(ImportArtnoType)).Cast<ImportArtnoType>().Select(x => new
            {
                label = x.Localize(),
                value = ((int)x).ToString(),
            });

            var importNameTypeList = Enum.GetValues(typeof(ImportNameType)).Cast<ImportNameType>().Select(x => new
            {
                label = x.Localize(),
                value = ((int)x).ToString(),
            });

            var useIn1CList = new List<bool> { false, true }.Select(x => new
            {
                label = x ? "Только с признаком \"Выгружать заказ в 1С\"" : "Все заказы",
                value = x.ToString().ToLower(),
            });

            var itemsPerPageList = new List<int> { 10, 20, 50, 100 }.Select(x => new
            {
                label = x.ToString(),
                value = x.ToString(),
            });

            var customerGroupList = CustomerGroupService.GetCustomerGroupList().Select(x => new
            {
                label = x.GroupName,
                value = x.CustomerGroupId.ToString(),
            });

            var tmp2 = TriggerCategoryService.GetList().OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToList();
            tmp2.Insert(0, new TriggerCategory { Id = 0, Name = "не использовать" });
            var triggerCategoryList = tmp2.Select(x => new
            {
                label = x.Name,
                value = x.Id.ToString(),
            });

            var tmp3 = ManagerService.GetManagersList(false).Where(x => !x.Enabled).ToList();
            tmp3.Insert(0, new Manager { ManagerId = 0 });
            var managerForDeleteList = tmp3.Select(x => new
            {
                label = x.FullName,
                value = x.ManagerId.ToString(),
            });

            //var e = ((int?)null).Value;

            return Json(new
            {
                Settings = settings,
                ApiKey = SettingsApi.ApiKey,
                DefaultCategoryList = defaultCategoryList,
                ImportArtnoTypeList = importArtnoTypeList,
                ImportNameTypeList = importNameTypeList,
                UseIn1CList = useIn1CList,
                ItemsPerPageList = itemsPerPageList,
                CustomerGroupList = customerGroupList,
                SendOrdersFromDate = DateTime.Today.ToString("yyyy-MM-dd"),
                TriggerCategoryList = triggerCategoryList,
                ManagerForDeleteList = managerForDeleteList
            });
        }

        [HttpPost]
        public JsonResult SaveSettings(ImportExportSettingsModel settings)
        {
            ModuleSettingsProvider.SetSettingValue("ImportExportSettings", JsonConvert.SerializeObject(settings), OneSApi.ModuleStringId);

            return Json(new { success = true, msg = "Сохранено" }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Products

        [HttpPost]
        public JsonResult ImportProductsFromFile(HttpPostedFileBase file)
        {
            if (file != null)
            {
                var fileName = file.FileName;

                if (string.IsNullOrEmpty(fileName))
                {
                    return JsonError("Недопустимый тип файла");
                }
                if (!(new List<string>() { ".json" }).Contains(Path.GetExtension(fileName.ToLower())))
                {
                    return JsonError("Недопустимый тип файла");
                }
            }

            //var path = FoldersHelper.GetPathAbsolut(FolderType.UserFiles, OneSApi.ModuleStringId);
            //FileHelpers.CreateDirectory(path);
            //path += "\\" + fileName;
            //if (System.IO.File.Exists(path))
            //{
            //    System.IO.File.Delete(path);
            //}
            //try
            //{
            //    file.SaveAs(path);
            //}
            //catch (Exception E)
            //{
            //    Debug.Log.Error(E);
            //    return JsonError("Ошибка файла загрузки");
            //}

            if (ModuleStatistic.IsRun)
            {
                return JsonError("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }

            Debug.Log.Info("ImportProductsFromFile");
            CatalogImportModel model;
            if (file != null)
            {
                try
                {
                    var json = new StreamReader(file.InputStream).ReadToEnd();
                    model = JsonConvert.DeserializeObject<CatalogImportModel>(json);
                }
                catch (Exception e)
                {
                    //ModelState.AddModelError("Error", e.Message);
                    return JsonError(e.Message);
                }
            }
            else
            {
                try
                {
                    var filename = System.Web.Hosting.HostingEnvironment.MapPath(string.Format("~/userfiles/modules/{0}/export_products.json", OneSApi.ModuleStringId));
                    Debug.Log.Info(filename);
                    var json = new StreamReader(filename).ReadToEnd();
                    model = JsonConvert.DeserializeObject<CatalogImportModel>(json);
                }
                catch (Exception e)
                {
                    //ModelState.AddModelError("Error", e.Message);
                    return JsonError(e.Message);
                }
            }

            Debug.Log.Info("ImportProductsFromFile Init");
            ModuleStatistic.Init();
            //ModuleStatistic.IsRun = true;
            //ModuleStatistic.CurrentProcess = "ImportProductsFromFile";
            //ModuleStatistic.CurrentProcessName = "Загрузка товаров из файла";
            ModuleStatistic.StartNew(() =>
            {
                //if (model.Products != null)
                //    ModuleStatistic.TotalRow += model.Products.Count;
                //if (model.Offers != null)
                //    ModuleStatistic.TotalRow += model.Offers.Count;
                //if (model.Photos != null)
                //    ModuleStatistic.TotalRow += model.Photos.Count;
                //if (model.Files != null)
                //    ModuleStatistic.TotalRow += model.Files.Count;

                try
                {
                    new ImportProducts(true, "ImportProductsFromFile", "Загрузка товаров из файла").Execute(model);
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                }

                ModuleStatistic.IsRun = false;
            });

            //try
            //{
            //    var result = new ImportProducts().Execute(model);
            return JsonOk();
            //}
            //catch (BlException e)
            //{
            //    ModelState.AddModelError(e.Property, e.Message);
            //    return JsonError();
            //}
        }

        [HttpPost]
        public JsonResult ImportRedirects(string filename)
        {
            if (filename.IsNullOrEmpty())
            {
                return JsonError("Не задано имя файла");
            }

            if (ModuleStatistic.IsRun)
            {
                return JsonError("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }

            ModuleStatistic.Init();
            ModuleStatistic.IsRun = true;
            ModuleStatistic.CurrentProcess = "ImportRedirects";
            ModuleStatistic.CurrentProcessName = "Загрузка редиректов";
            ModuleStatistic.StartNew(() =>
            {
                try
                {
                    new ImportRedirects(filename).Execute(true);
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                }

                ModuleStatistic.IsRun = false;
            });

            return JsonOk();
        }

        #endregion

        #region Orders

        [HttpPost]
        public JsonResult SendOrders(string fromDate, string toDate)
        {
            var from = DateTime.ParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var to = toDate.IsNotEmpty() ? DateTime.ParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.Now;
            var ids = ExportService.GetAllOrderIds(from, to);
            ExportService.SendOrders(ids);

            return JsonOk();
        }

        public JsonResult GetOrderList(OrderListFilterModel filter)
        {
            var logs = new GetOrderList(filter).Execute();

            return Json(logs);
        }

        [HttpPost]
        public JsonResult DeleteOrderFromExport(int orderId, int exportType)
        {
            try
            {
                ExportService.DeleteOrderFromExport(orderId, exportType);
            }
            catch (Exception E)
            {
                return Json(new { result = false });
            }

            return Json(new { result = true });
        }

        [HttpPost]
        public JsonResult DeleteOrdersFromExport(OrderListFilterModel model)
        {
            //Command_Catalog(model, (id, c) => OzonCatalogService.DeleteProductFromCategory(id));
            var orders = ExportService.GetSelected(model);
            foreach (var order in orders)
            {
                ExportService.DeleteOrderFromExport(order.OrderId, order.ExportType);
            }
            return JsonOk();
        }

        [HttpPost]
        public JsonResult ImportOrdersFromFile(HttpPostedFileBase file)
        {
            var fileName = file.FileName;

            if (string.IsNullOrEmpty(fileName))
            {
                return JsonError("Недопустимый тип файла");
            }
            if (!(new List<string>() { ".json" }).Contains(Path.GetExtension(fileName.ToLower())))
            {
                return JsonError("Недопустимый тип файла");
            }

            if (ModuleStatistic.IsRun)
            {
                return JsonError("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }

            OrderImportModel model;
            try
            {
                var json = new StreamReader(file.InputStream).ReadToEnd();
                model = JsonConvert.DeserializeObject<OrderImportModel>(json);
            }
            catch (Exception e)
            {
                return JsonError(e.Message);
            }

            ModuleStatistic.Init();
            ModuleStatistic.IsRun = true;
            ModuleStatistic.CurrentProcess = "ImportOrdersFromFile";
            ModuleStatistic.CurrentProcessName = "Загрузка заказов из файла";
            ModuleStatistic.StartNew(() =>
            {
                if (model.Orders != null)
                    ModuleStatistic.TotalRow += model.Orders.Count;

                try
                {
                    new ImportOrders(true).Execute(model);
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                }

                ModuleStatistic.IsRun = false;
            });

            return JsonOk();
        }

        [HttpPost]
        public JsonResult ExportOrdersToFile(OrderListFilterModel filter)
        {
            if (ModuleStatistic.IsRun)
            {
                return JsonError("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }

            ModuleStatistic.Init();
            ModuleStatistic.IsRun = true;
            ModuleStatistic.CurrentProcess = "ExportOrdersToFile";
            ModuleStatistic.CurrentProcessName = "Выгрузка заказов в файл";
            ModuleStatistic.StartNew(() =>
            {
                var settings = ModuleService.GetImportExportSettings();
                var orders = ExportService.GetOrderList(filter);

                ModuleStatistic.TotalRow = orders.Count;

                try
                {
                    var model = new List<OrderV8Model>();

                    foreach (var order in orders)
                    {
                        var f = new FilterOrdersModel
                        {
                            OrderId = order.OrderId,
                            LoadItems = true,
                            ItemsPerPage = 1,
                            //Page = (int)ModuleStatistic.RowPosition + 1
                        };
                        var handler = new GetOrders(f, settings);
                        var items = handler.Execute();

                        model.Add(items.DataItems[0]);
                        ModuleStatistic.RowPosition++;
                    }

                    var json = JsonConvert.SerializeObject(model);
                    var folder = FoldersHelper.GetPathAbsolut(FolderType.PriceTemp);
                    var filename = "import_orders_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json";
                    using (var wr = new StreamWriter(folder + "\\" + filename))
                    {
                        wr.Write(json);
                    }

                    ModuleStatistic.FileName = FoldersHelper.GetPath(FolderType.PriceTemp, filename, true);
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                }

                ModuleStatistic.IsRun = false;
            });

            return JsonOk();
        }

        #endregion

        #region Customers

        [HttpPost]
        public JsonResult ImportCustomersFromFile(HttpPostedFileBase file)
        {
            var fileName = file.FileName;

            if (string.IsNullOrEmpty(fileName))
            {
                return JsonError("Недопустимый тип файла");
            }
            if (!(new List<string>() { ".json" }).Contains(Path.GetExtension(fileName.ToLower())))
            {
                return JsonError("Недопустимый тип файла");
            }

            if (ModuleStatistic.IsRun)
            {
                return JsonError("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }

            CustomersImportModel model;
            try
            {
                var json = new StreamReader(file.InputStream).ReadToEnd();
                model = JsonConvert.DeserializeObject<CustomersImportModel>(json);
            }
            catch (Exception e)
            {
                return JsonError(e.Message);
            }

            ModuleStatistic.Init();
            ModuleStatistic.IsRun = true;
            ModuleStatistic.CurrentProcess = "ImportCustomersFromFile";
            ModuleStatistic.CurrentProcessName = "Загрузка покупателей из файла";
            ModuleStatistic.StartNew(() =>
            {
                if (model.Customers != null)
                    ModuleStatistic.TotalRow += (model.Managers ?? new List<AddUpdateManagerModel>()).Count + (model.Customers ?? new List<AddUpdateCustomerModel>()).Count;

                try
                {
                    new ImportCustomers(true).Execute(model);
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                }

                ModuleStatistic.IsRun = false;
            });

            return JsonOk();
        }

        [HttpPost]
        public JsonResult DeleteManagerFromOrders(int managerId)
        {
            var result = new DeleteManagerFromOrders().Execute(managerId);

            if (result.IsNullOrEmpty())
                return JsonOk();
            else
                return JsonError(result);
        }

        public JsonResult GetCustomerList(CustomerListFilterModel filter)
        {
            var logs = new GetCustomerList(filter).Execute();

            return Json(logs);
        }

        #endregion

        #region Products

        public JsonResult GetProductList(ProductListFilterModel filter)
        {
            var logs = new GetProductList(filter).Execute();

            return Json(logs);
        }

        [HttpPost]
        public JsonResult DeleteProductFromExport(int productId, int exportType)
        {
            try
            {
                ExportService.DeleteProductFromExport(productId, exportType);
            }
            catch (Exception E)
            {
                return Json(new { result = false });
            }

            return Json(new { result = true });
        }

        [HttpPost]
        public JsonResult ExportProductsToFile(ProductListFilterModel filter)
        {
            if (ModuleStatistic.IsRun)
            {
                return JsonError("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }

            ModuleStatistic.Init();
            ModuleStatistic.IsRun = true;
            ModuleStatistic.CurrentProcess = "ExportProductsToFile";
            ModuleStatistic.CurrentProcessName = "Выгрузка товаров в файл";
            ModuleStatistic.StartNew(() =>
            {
                var settings = ModuleService.GetImportExportSettings();
                var products = ExportService.GetProductList(filter);

                ModuleStatistic.TotalRow = products.Count;

                try
                {
                    var model = new List<ProductExportModel>();

                    foreach (var product in products)
                    {
                        var f = new FilterProductsModel
                        {
                            ProductId = product.ProductId,
                            All = true
                        };
                        var handler = new GetProducts(f, settings);
                        var items = handler.Execute();

                        model.Add(items.DataItems[0]);
                        ModuleStatistic.RowPosition++;
                    }

                    var json = JsonConvert.SerializeObject(model);
                    var folder = FoldersHelper.GetPathAbsolut(FolderType.PriceTemp);
                    var filename = "import_products_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json";
                    using (var wr = new StreamWriter(folder + "\\" + filename))
                    {
                        wr.Write(json);
                    }

                    ModuleStatistic.FileName = FoldersHelper.GetPath(FolderType.PriceTemp, filename, true);
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                }

                ModuleStatistic.IsRun = false;
            });

            return JsonOk();
        }

        #endregion

        #region DepotsAmounts

        [HttpGet]
        public JsonResult GetDepots()
        {
            return Json(new GetDepots().Execute());
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult InplaceDepot(DepotModel depot)
        {
            return ProcessJsonResult(new AddUpdateDepot(depot));
        }

        [HttpGet]
        public JsonResult GetDepot(int id)
        {
            var depot = DepotAmountsService.GetDepot(id);
            if (depot == null)
                depot = new DepotModel { DepartmentId = 0 };
            if (!depot.DepartmentId.HasValue)
                depot.DepartmentId = 0;

            var departments = DepartmentService.GetDepartmentsList().Where(x => x.Enabled)
                .Select(x => new SelectItemModel(x.Name, x.DepartmentId)).ToList();

            return Json(new { depot, departments });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddDepot(DepotModel depot)
        {
            return ProcessJsonResult(new AddUpdateDepot(depot));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult UpdateDepot(DepotModel depot)
        {
            return ProcessJsonResult(new AddUpdateDepot(depot));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult DeleteDepot(int id)
        {
            return ProcessJsonResult(() => DepotAmountsService.DeleteDepot(id));
        }

        public ActionResult ProductDepotsAmounts(int productId)
        {
            var model = new DepotsProductModel() { ProductId = productId };
            model.Amounts = DepotAmountsService.GetAmounts(productId);

            return PartialView("~/Modules/" + OneSApi.ModuleStringId + "/Views/Admin/Product/_DepotsAmounts.cshtml", model);
        }

        [HttpPost]
        public ActionResult ProductDepotsAmounts(DepotsProductModel model)
        {
            DepotAmountsService.UpadateProductAmounts(model.ProductId, model.Amounts);

            return ProductDepotsAmounts(model.ProductId);
        }

        #endregion

        #region ShippingMethods

        [HttpGet]
        public JsonResult GetShippingMethods()
        {
            return Json(new GetShippingMethods().Execute());
        }

        [HttpGet]
        public JsonResult GetShippingMethod(string key)
        {
            ShippingMethodModel method = key.IsNotEmpty() ? ShippingMethodsService.GetShippingMethod(key) : null;
            if (method == null)
                method = new ShippingMethodModel();

            var shippingMethodKeys = AdvantshopConfigService.GetDropdownShippings().Select(x => new { label = x.Text, value = x.Value }).ToList();
            shippingMethodKeys.AddRange(ModulesExecuter.GetDropdownShippings().Select(x => new { label = x.Text, value = x.Value }));

            return Json(new { method, shippingMethodKeys });
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult AddUpdateShippingMethod(ShippingMethodModel method)
        {
            return ProcessJsonResult(new AddUpdateShippingMethod(method));
        }

        [HttpPost, ValidateJsonAntiForgeryToken]
        public JsonResult InplaceShippingMethod(ShippingMethodModel method)
        {
            return ProcessJsonResult(new AddUpdateShippingMethod(method));
        }

        [HttpPost]
        public JsonResult CalculateShippings(HttpPostedFileBase file)
        {
            var fileName = file.FileName;

            if (string.IsNullOrEmpty(fileName))
            {
                return JsonError("Недопустимый тип файла");
            }
            if (!(new List<string>() { ".json" }).Contains(Path.GetExtension(fileName.ToLower())))
            {
                return JsonError("Недопустимый тип файла");
            }

            if (ModuleStatistic.IsRun)
            {
                return JsonError("Уже запущен другой процесс: " + ModuleStatistic.CurrentProcessName);
            }

            GetCheckoutShippingsRequest model;
            try
            {
                var json = new StreamReader(file.InputStream).ReadToEnd();
                model = JsonConvert.DeserializeObject<GetCheckoutShippingsRequest>(json);
            }
            catch (Exception e)
            {
                return JsonError(e.Message);
            }

            ModuleStatistic.Init();
            ModuleStatistic.IsRun = true;
            ModuleStatistic.CurrentProcess = "CalculateShippings";
            ModuleStatistic.CurrentProcessName = "Тест расчета доставки";
            ModuleStatistic.StartNew(() =>
            {
                if (model != null)
                    ModuleStatistic.TotalRow = 1;

                try
                {
                    new GetCheckoutShippings(model, true).Execute();
                }
                catch (Exception E)
                {
                    Debug.Log.Error(E);
                }

                ModuleStatistic.IsRun = false;
            });

            return JsonOk();
        }

        #endregion

        #region Logs

        public JsonResult GetLogs(string folders, BaseFilterModel filter)
        {
            var logs = new GetLogs(folders, filter).Execute();

            return Json(logs);
        }

        public FileResult DownloadLog(string filename, string folder)
        {
            var file = System.Web.Hosting.HostingEnvironment.MapPath(string.Format("~/userfiles/modules/{0}/{1}/{2}.json", OneSApi.ModuleStringId, folder, filename));
            if (!System.IO.File.Exists(file))
                return null;

            return File(file, System.Net.Mime.MediaTypeNames.Application.Octet, filename + ".txt");
        }

        #endregion

        #region common

        [HttpPost]
        public JsonResult GetImportStatistic()
        {
            return JsonOk(new ImportStatisticModel()
            {
                IsRun = ModuleStatistic.IsRun,
                Process = ModuleStatistic.CurrentProcess,
                ProcessName = ModuleStatistic.CurrentProcessName,
                ProgressValue = ModuleStatistic.RowPosition.ToString(),
                ProgressTotal = ModuleStatistic.TotalRow.ToString(),
                ErrorMessage = ModuleStatistic.ErrorMessage,
                FileName = ModuleStatistic.FileName
            });
        }

        [HttpPost]
        public JsonResult BreakProcess()
        {
            ModuleStatistic.CurrentProcess = "";
            ModuleStatistic.CurrentProcessName = "";
            //BeruStatistic.TotalRow = _orders.Count;
            //BeruStatistic.RowPosition = 0;
            ModuleStatistic.IsRun = false;

            return JsonOk();
        }

        #endregion

    }
}
