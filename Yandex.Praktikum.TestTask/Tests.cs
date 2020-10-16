using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using System;
using System.Collections.Generic;

/*Задача 3 (опциональная, третий уровень сложности)*/

/*Напиши автотесты, которые покроют написанный код. Тестовая модель для задания - результаты первого уровня.*/

namespace Yandex.Praktikum.TestTask.Tests
{
    #region utils

    public static class Matchers
    {
        public static void AssertSuccessAndSum(this CalcResponse response, double sum)
        {
            Assert.That(response.success.Value, "Условия выдачи кредита не выполнены");
            Assert.AreEqual(sum, response.yearPayment.Value, 0.01, "Ожидаемая и расчитанная сумма годового платежа не совпали");
        }
    }

    public class CreditCalculatorArgumentException : Exception { }

    #endregion

    #region business objects

    public class CalcRequest
    {
        public int? age;

        public string gender;

        public string type;

        public int? annual;

        public int? rating;

        public double? amount;

        public int? period;

        public string goal; 
    }

    public class CalcResponse
    {
        public bool? success;

        public double? yearPayment;
        
        public string message;
    }

    #endregion

    #region service implementation

    public class CreditCalculatorAPI
    {
        public string URL;

        public RestClient RestClient;

        public CreditCalculatorAPI()
        {
            RestClient = new RestClient();
            URL = "https://www.wolframcloud.com/obj/kirillbelovtest/Deploy/Yandex.Praktikum/CreditCalculator/API/f7426666d3a1d26615e2f6568813bc6d910fecf3";
        }

        public CalcResponse Calc(object body)
        {
            RestRequest request = new RestRequest(URL, Method.POST, DataFormat.Json);
            request.AddJsonBody(body);
            IRestResponse response = RestClient.Execute(request);
            CalcResponse result = JsonConvert.DeserializeObject<CalcResponse>(response.Content);
            if (result.message == "argument exception") throw new CreditCalculatorArgumentException();
            return result;
        }
    }

    #endregion

    #region test data provider

    public class TestData
    {
        /*таблица 1*/
        /*
            | age | gender | type     | annual | amount | rating | period | goal     | expected |
            | --- | ------ | -------- | ------ | ------ | ------ | ------ | -------- | -------- |
            | 1   | f      | passive  | 1      | 0.1    | -1     | 5      | mortgage | 0.031    |
            | 18  | m      | employee | 30     | 10     | 0      | 20     | business | 0.677551 |
            | 57  | f      | business | 22     | 9      | 1      | 2      | car      | 5.44912  |
            | 63  | m      | employee | 1      | 0.2    | 2      | 1      | consumer | 0.222398 |
         */
        public static IEnumerable<object[]> TestDataSuccess()
        {
            yield return new object [] {new CalcRequest { age =  1, annual =  1, rating = -1, gender = "F", goal = "mortgage", type =  "passive", period =  5, amount = 0.1 },    0.031 };
            yield return new object [] {new CalcRequest { age = 18, annual = 30, rating =  0, gender = "M", goal = "business", type = "employee", period = 20, amount =  10 }, 0.677551 };
            yield return new object [] {new CalcRequest { age = 57, annual = 22, rating =  1, gender = "F", goal =      "car", type = "business", period =  2, amount =   9 },  5.44912 };
            yield return new object [] {new CalcRequest { age = 63, annual =  1, rating =  2, gender = "M", goal = "consumer", type = "employee", period =  1, amount = 0.2 }, 0.222398 };
        }

        /*таблица 2*/
        /*
            | denied by  | age    | gender | type           | annual | amount | rating | period | goal     |
            | ---------- | ------ | ------ | -------------- | ------ | ------ | ------ | ------ | -------- |
            | age+gender | **59** | **f**  | passive        | 1      | 0.1    | -1     | 5      | mortgage |
            | age+gender | **60** | **m**  | employee       | 30     | 10     | 0      | 20     | business |
            | age        | **70** | f      | business       | 22     | 9      | 1      | 2      | car      |
            | type       | 63     | m      | **unemployed** | 1      | 0.2    | 2      | 1      | consumer |
            | rating     | 1      | f      | passive        | 1      | 0.1    | **-2** | 5      | mortgage |
            | annual     | 18     | m      | employee       | **-1** | 10     | 0      | 20     | business |
            | annual+sum | 57     | f      | business       | **22** | **10** | 1      | 2      | car      |
         */
        public static IEnumerable<object[]> TestDataDenied()
        {
            yield return new[] { new CalcRequest { age = 59, annual =  1, rating = -1, gender = "F", goal = "mortgage", type =    "passive", period =  5, amount = 0.1 } };
            yield return new[] { new CalcRequest { age = 60, annual = 30, rating =  0, gender = "M", goal = "business", type =   "employee", period = 20, amount =  10 } };
            yield return new[] { new CalcRequest { age = 70, annual = 22, rating =  1, gender = "F", goal =      "car", type =   "business", period =  2, amount =   9 } };
            yield return new[] { new CalcRequest { age = 63, annual =  1, rating =  2, gender = "M", goal = "consumer", type = "unemployed", period =  1, amount = 0.2 } };
            yield return new[] { new CalcRequest { age =  1, annual =  1, rating = -2, gender = "F", goal = "mortgage", type =    "passive", period =  5, amount = 0.1 } };
            yield return new[] { new CalcRequest { age = 18, annual = -1, rating =  0, gender = "M", goal = "business", type =   "employee", period = 20, amount =  10 } };
            yield return new[] { new CalcRequest { age = 57, annual = 22, rating =  1, gender = "F", goal =      "car", type =   "business", period =  1, amount =  10 } };
        }

        /*таблица 3*/
        /*
            | denied by        | age    | gender | type        | annual       | amount | rating | period | goal     |
            | ---------------- | ------ | ------ | ----------- | ------------ | ------ | ------ | ------ | -------- |
            | age              | **-1** | f      | passive     | 1            | 0.1    | -1     | 5      | mortgage |
            | gender           | 20     | **x**  | employee    | 30           | 10     | 0      | 20     | business |
            | type             | 70     | f      | **testing** | 22           | 9      | 1      | 2      | car      |
            | annual           | 63     | m      | unemployed  | **infinity** | 0.2    | 2      | 1      | consumer |
            | amount           | 1      | f      | passive     | 1            | **11** | -2     | 5      | mortgage |
            | rating           | 18     | m      | employee    | -1           | 10     | **-3** | 20     | business |
            | goal             | 57     | f      | business    | 22           | 10     | 1      | 2      | startup  |
            | age              |        | m      | employee    | -1           | 10     | -1     | 20     | business |
            | annual -> income | 57     | f      | business    | **replaced** | 10     | 1      | 2      | startup  |
        */
        public static IEnumerable<object[]> TestDataNegative()
        {
            yield return new[] { new CalcRequest { age = -1, gender = "f", type =    "passive", annual =          1, amount = 0.1, rating = -1, period =  5, goal = "mortgage" } };
            yield return new[] { new CalcRequest { age = 20, gender = "x", type =   "employee", annual =         30, amount =  10, rating =  0, period = 20, goal = "business" } };
            yield return new[] { new CalcRequest { age = 70, gender = "f", type =    "testing", annual =         22, amount =   9, rating =  1, period =  2, goal =      "car" } };
            yield return new[] { new             { age = 63, gender = "m", type = "unemployed", annual = "infinity", amount = 0.2, rating =  2, period =  1, goal = "consumer" } };
            yield return new[] { new CalcRequest { age =  1, gender = "f", type =    "passive", annual =          1, amount =  11, rating = -2, period =  5, goal = "mortgage" } };
            yield return new[] { new CalcRequest { age = 18, gender = "m", type =   "employee", annual =         -1, amount =  10, rating = -3, period = 20, goal = "business" } };
            yield return new[] { new CalcRequest { age = 57, gender = "f", type =   "business", annual =         22, amount =  10, rating =  1, period =  2, goal =  "startup" } };
            yield return new[] { new CalcRequest {           gender = "m", type =   "employee", annual =         -1, amount =  10, rating = -1, period = 20, goal = "business" } };
            yield return new[] { new             { age = 57, gender = "f", type =   "business", replaced =        0, amount =  10, rating =  1, period =  2, goal = "business" } };
        }
    }

    #endregion

    #region ws tests

    [TestFixture]
    public class Tests
    {
        public CreditCalculatorAPI CreditCalculator;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            CreditCalculator = new CreditCalculatorAPI();
        }

        [TestCaseSource(typeof(TestData), "TestDataSuccess")]
        public void Test_01_Success(CalcRequest input, double output)
        {
            CreditCalculator.Calc(input).AssertSuccessAndSum(output);
        }

        [TestCaseSource(typeof(TestData), "TestDataDenied")]
        public void Test_02_Denied(CalcRequest input)
        {
            Assert.That(!CreditCalculator.Calc(input).success.Value, "Ожидаемый отказ в кредите не вернулся");
        }

        [TestCaseSource(typeof(TestData), "TestDataNegative")]
        public void Test_03_Negative(object input)
        {
            Assert.Throws<CreditCalculatorArgumentException>(() => CreditCalculator.Calc(input), "Ожидаемое исключение не появилось");
        }
    }

    #endregion
}
