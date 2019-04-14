using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Orleans.Hosting;
using OrleansClient;
using SiloHost;

namespace webapi.Controllers
{
    // [Route("api/[controller]")]
    public class ResourcesController : Controller
    {
        [Route("api/resources/operator-metadata")]
        public IActionResult OperatorMetadata()
        {
            string ret = "{\"operators\": [ { \"operatorType\": \"ScanSource\", \"jsonSchema\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:source:scan:ScanSourcePredicate\", \"properties\": { \"tableName\": { \"type\": \"string\" } }, \"required\": [ \"tableName\" ] }, \"additionalMetadata\": { \"userFriendlyName\": \"Source: Scan\", \"operatorDescription\": \"Read records from a table one by one\", \"operatorGroupName\": \"Source\", \"numInputPorts\": 0, \"numOutputPorts\": 1, \"advancedOptions\": [] } }, { \"operatorType\": \"Comparison\", \"jsonSchema\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:comparablematcher:ComparablePredicate\", \"properties\": { \"attributeName\": { \"type\": \"string\" }, \"comparisonType\": { \"type\": \"string\", \"enum\": [ \"=\", \">\", \">=\", \"<\", \"<=\", \"\u2260\" ] }, \"compareTo\": { \"type\": \"any\" } }, \"required\": [ \"attributeName\", \"comparisonType\", \"compareTo\" ] }, \"additionalMetadata\": { \"userFriendlyName\": \"Comparison\", \"operatorDescription\": \"Select data based on a condition (>, <, =, ..)\", \"operatorGroupName\": \"Utilities\", \"numInputPorts\": 1, \"numOutputPorts\": 1, \"advancedOptions\": [] } }, { \"operatorType\": \"Aggregation\", \"jsonSchema\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:aggregator:AggregatorPredicate\", \"properties\": { \"listOfAggregations\": { \"type\": \"array\", \"items\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:aggregator:AggregationAttributeAndResult\", \"properties\": { \"attribute\": { \"type\": \"string\" }, \"aggregator\": { \"type\": \"string\", \"enum\": [ \"min\", \"max\", \"average\", \"sum\", \"count\" ] }, \"resultAttribute\": { \"type\": \"string\" } } } } }, \"required\": [ \"listOfAggregations\" ] }, \"additionalMetadata\": { \"userFriendlyName\": \"Aggregation\", \"operatorDescription\": \"Aggregate one or more columns to find min, max, sum, average, count of the column\", \"operatorGroupName\": \"Utilities\", \"numInputPorts\": 1, \"numOutputPorts\": 1, \"advancedOptions\": [] } }, { \"operatorType\": \"KeywordMatcher\", \"jsonSchema\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:keywordmatcher:KeywordPredicate\", \"properties\": { \"attributeName\": { \"type\": \"string\" }, \"keyword\":{\"type\":\"string\"} }, \"required\": [ \"attributeName\", \"keyword\" ] }, \"additionalMetadata\": { \"userFriendlyName\": \"Keyword Search\", \"operatorDescription\": \"Search the documents using a keyword\", \"operatorGroupName\": \"Search\", \"numInputPorts\": 1, \"numOutputPorts\": 1, \"advancedOptions\": [] } },{\"operatorType\": \"CrossRippleJoin\",\"jsonSchema\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:join:crossjoin\", \"properties\": {\"batchingLimit\": { \"type\": \"string\" } }, \"required\": [] }, \"additionalMetadata\": { \"userFriendlyName\": \"Cross Join\", \"operatorDescription\": \"Produce the Cartesian Product of two sources\", \"operatorGroupName\": \"Join\", \"numInputPorts\": 2, \"numOutputPorts\": 1, \"advancedOptions\": [] }},{\"operatorType\": \"HashRippleJoin\",\"jsonSchema\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:join:fulljoin\", \"properties\": { \"attributeName\": { \"type\": \"string\" }}, \"required\": [\"attributeName\"] }, \"additionalMetadata\": { \"userFriendlyName\": \"Full Join\", \"operatorDescription\": \"Do full join on two scources based on one condition\", \"operatorGroupName\": \"Join\", \"numInputPorts\": 2, \"numOutputPorts\": 1, \"advancedOptions\": [] }},{\"operatorType\": \"InsertionSort\",\"jsonSchema\": { \"type\": \"object\", \"id\": \"urn:jsonschema:edu:uci:ics:texera:dataflow:sort:insertionsort\", \"properties\": { \"attributeName\": { \"type\": \"string\" }}, \"required\": [\"attributeName\"] }, \"additionalMetadata\": { \"userFriendlyName\": \"Sort\", \"operatorDescription\": \"Insertion Sort\", \"operatorGroupName\": \"Sort\", \"numInputPorts\": 1, \"numOutputPorts\": 1, \"advancedOptions\": [] }} ], \"groups\": [ { \"groupName\": \"Source\", \"groupOrder\": 0 }, { \"groupName\": \"Search\", \"groupOrder\": 1 },{ \"groupName\": \"Join\", \"groupOrder\": 2 },{ \"groupName\": \"Sort\", \"groupOrder\": 3 },{ \"groupName\": \"Utilities\", \"groupOrder\": 4} ] }";
            JObject json = JObject.Parse(ret);
            return Json(json);
        }
    }
}
