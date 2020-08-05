(* ::Package:: *)

$HistoryLength = 0; 
ClearAll["`*"]
SetDirectory[NotebookDirectory[]];


$incomeTypes = "passive" | "employee" | "business" | "unemployed";


$creditRatings = -2 | -1 | 0 | 1 | 2;


availableAmountQ = NumericQ[#] && 0.1 <= # <= 10.0&;


availablePeriodQ = IntegerQ[#] && 1 <= # <= 20&;


$goals = "mortgage" | "business" | "car" | "consumer";


retirementAge = <|"f" -> 60, "m" -> 65|>; 


maxAmount[incomeType: $incomeTypes, creditRating: $creditRatings] := 
	Min[
		incomeType /. {"passive" -> 1, "employee" -> 5, "business" -> 10}, 
		creditRating /. {-1 -> 1, 0 -> 5, 1 -> 10, 2 -> 10}
	]


$baseRate = 10; 


modificators[incomeType: $incomeTypes, creditRating: $creditRatings, requaredAmount_?availableAmountQ, goal: $goals] := 
	Total[{
		incomeType /. {"passive" -> 0.5, "employee" -> -0.25, "business" -> 0.25}, 
		creditRating /. {-1 -> 1.5, 0 -> 0, 1 -> -0.25, 2 -> -0.75}, 
		-Log10[requaredAmount], 
		goal /. {"mortgage" -> -2, "business" -> -0.5, "car" -> 1.5, "consumer" -> 1.5}
	}]


yearPayment[incomeType: $incomeTypes, annualIncome_Integer, creditRating: $creditRatings, 
	requaredAmount_?availableAmountQ, period_?availablePeriodQ, goal: $goals] := 
	(requaredAmount*(1 + period*($baseRate + modificators[incomeType, creditRating, requaredAmount, goal])/100))/period


CreditCalculator::argex = 
"argument exception";


CreditCalculator[
	age_Integer?Positive, 
	gender: "f" | "m", 
	incomeType: $incomeTypes, 
	annualIncome_Integer, 
	creditRating: $creditRatings, 
	requaredAmount_?availableAmountQ, 
	period_?availablePeriodQ, 
	goal: $goals] := 
	Block[{$yearPayment, $maxAmount}, 
		Which[
			age + period > retirementAge[gender], 
				<|"success" -> False|>, 
				
			requaredAmount / period > annualIncome / 3, 
				<|"success" -> False|>, 
				
			creditRating === -2, 
				<|"success" -> False|>, 
				
			incomeType === "unemployed", 
				<|"success" -> False|>, 
				
			$maxAmount = maxAmount[incomeType, creditRating]; 
			$yearPayment = yearPayment[incomeType, annualIncome, creditRating, 
				Min[$maxAmount, requaredAmount], period, goal]; 
			$yearPayment > annualIncome / 2, 	
				<|"success" -> False|>, 
			
			$maxAmount = maxAmount[incomeType, creditRating]; 
			True, 
				<|"success" -> True, "yearPayment" -> 
					Chop @ yearPayment[incomeType, annualIncome, creditRating, 
						Min[$maxAmount, requaredAmount], period, goal]|>
		]
	]


CreditCalculator[assoc_?AssociationQ] := 
	CreditCalculator[#age, #gender, #type, #annual, #rating, #amount, #period, #goal]& @ 
	If[Length[#] === 8, #, Null]& @ 
	Association @ 
	KeyValueMap[ToLowerCase[#1] -> If[StringQ[#2], ToLowerCase[#2], #2]&] @ assoc


CreditCalculator[args___] := 
	<|"message" -> (Message[CreditCalculator::argex]; StringTemplate[CreditCalculator::argex][{args}])|>


$creditCalculatorAPI = 
	CloudDeploy[
		APIFunction[{}, CreditCalculator[ImportString[HTTPRequestData["Body"], "RawJSON"]]&, "RawJSON"],
        "Deploy/Yandex.Praktikum/CreditCalculator/API/" <> Hash[CreditCalculator, "SHA", "HexString"], 
        Permissions -> "Public"
    ]; 


$creditCalculatorForm = 
	CloudDeploy[
		FormPage[{
			{"age", "\:0412\:043e\:0437\:0440\:0430\:0441\:0442, \:043b\:0435\:0442"} ->  Restricted["Integer", {1, 13799 * 10^6}], 
			{"gender", "\:041f\:043e\:043b"} -> {"F", "M"}, 
			{"type", "\:0418\:0441\:0442\:043e\:0447\:043d\:0438\:043a \:0434\:043e\:0445\:043e\:0434\:0430"} -> {
				"\:041f\:0430\:0441\:0441\:0438\:0432\:043d\:044b\:0439 \:0434\:043e\:0445\:043e" -> "passive", 
				"\:041d\:0430\:0435\:043c\:043d\:044b\:0439 \:0440\:0430\:0431\:043e\:0442\:043d\:0438\:043a" -> "employee", 
				"\:0421\:043e\:0431\:0441\:0442\:0432\:0435\:043d\:043d\:044b\:0439 \:0431\:0438\:0437\:043d\:0435\:0441" -> "business", 
				"\:0411\:0435\:0437\:0440\:0430\:0431\:043e\:0442\:043d\:044b\:0439" -> "unemployed"
			}, 
			{"annual", "\:0413\:043e\:0434\:043e\:0432\:043e\:0439 \:0434\:043e\:0445\:043e\:0434, \:043c\:043b\:043d. \:0440\:0443\:0431."} -> "Integer", 
			{"rating", "\:041a\:0440\:0435\:0434\:0438\:0442\:043d\:044b\:0439 \:0440\:0435\:0439\:0442\:0438\:043d\:0433"} -> {-2, -1, 0, 1, 2}, 
			{"amount", "\:0417\:0430\:043f\:0440\:043e\:0448\:0435\:043d\:0430\:044f \:0441\:0443\:043c\:043c\:0430, \:043c\:043b\:043d. \:0440\:0443\:0431."} -> Restricted["Real", {0.1, 10}], 
			{"period", "\:0421\:043a\:0440\:043e \:043f\:043e\:0433\:0430\:0448\:0435\:043d\:0438\:044f, \:043b\:0435\:0442"} -> Restricted["Integer", {1, 10}], 
			{"goal", "\:0426\:0435\:043b\:044c \:043a\:0440\:0435\:0434\:0438\:0442\:0430"} -> {"\:0418\:043f\:043e\:0442\:0435\:043a\:0430" -> "mortgage", "\:0420\:0430\:0437\:0432\:0438\:0442\:0438\:0435 \:0431\:0438\:0437\:043d\:0435\:0441\:0430" -> "business", 
			"\:0410\:0432\:0442\:043e\:043a\:0440\:0435\:0434\:0438\:0442\:043e\:0432\:0430\:043d\:0438\:0435" -> "car", "\:041f\:043e\:0442\:0440\:0435\:0431\:0438\:0442\:0435\:043b\:044c\:0441\:043a\:0438\:0439" -> "consumer"}
		}, Block[{res = CreditCalculator[#]}, 
			Which[
				res["success"], 
					Style["\:041e\:0434\:043e\:0431\:0440\:0435\:043d\:043e. \:0413\:043e\:0434\:043e\:0432\:043e\:0439 \:043f\:043b\:0430\:0442\:0435\:0436 = " <> ToString[res["yearPayment"]] <> " \:043c\:043b\:043d. \:0440\:0443\:0431.", "Subtitle"], 
				!res["success"], 
					Style["\:041e\:0422\:041a\:041b\:041e\:041d\:0415\:041d\:041e", "Subtitle", Gray], 
				KeyExistsQ[res, "message"], 
					Style[res["message"], "Subtitle", Red]
			]]&, AppearanceRules -> <|"ItemLayout" -> "Vertical"|>],
        "Deploy/Yandex.Praktikum/CreditCalculator/Form/" <> Hash[CreditCalculator, "SHA", "HexString"], 
        Permissions -> "Public"
    ]; 
