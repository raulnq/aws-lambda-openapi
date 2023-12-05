using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PetsApi;

public class Function
{
    private readonly AmazonDynamoDBClient _dynamoDBClient;
    private readonly string _tableName;
    private readonly JsonSerializerOptions _options;

    public Function()
    {
        _dynamoDBClient = new AmazonDynamoDBClient();
        _tableName = Environment.GetEnvironmentVariable("TABLE_NAME")!;
        _options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy= JsonNamingPolicy.CamelCase,
        };
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> Register(APIGatewayHttpApiV2ProxyRequest input, ILambdaContext context)
    {
        var request = JsonSerializer.Deserialize<RegisterPetRequest>(input.Body, _options)!;

        var petId = Guid.NewGuid();

        var putItemRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue> {
                {
                        "petId",
                        new AttributeValue {
                        S = petId.ToString(),
                    }
                    },
                    {
                        "type",
                        new AttributeValue {
                        S = request.Type
                        }
                    },
                    {
                        "name",
                        new AttributeValue {
                            S = request.Name
                        }
                }
            }
        };

        await _dynamoDBClient.PutItemAsync(putItemRequest);

        var body = JsonSerializer.Serialize(new RegisterPetResponse(petId), _options);

        return new APIGatewayHttpApiV2ProxyResponse
        {
            Body = body,
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } }
        };
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> Get(APIGatewayHttpApiV2ProxyRequest input, ILambdaContext context)
    {
        var petId = input.PathParameters["petId"];

        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>() { { "petId", new AttributeValue { S = petId.ToString() } } },
        };

        var response = await _dynamoDBClient.GetItemAsync(request);

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 404
            };
        }

        var body = JsonSerializer.Serialize(new Pet(Guid.Parse(response.Item["petId"].S), response.Item["type"].S, response.Item["name"].S), _options);

        return new APIGatewayHttpApiV2ProxyResponse
        {
            Body = body,
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } }
        };
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> List(APIGatewayHttpApiV2ProxyRequest input, ILambdaContext context)
    {
        var response = await _dynamoDBClient.ScanAsync(new ScanRequest() { TableName = _tableName});

        var body = JsonSerializer.Serialize(response.Items.Select(item=> new Pet(Guid.Parse(item["petId"].S), item["type"].S, item["name"].S)), _options);

        return new APIGatewayHttpApiV2ProxyResponse
        {
            Body = body,
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } }
        };
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> Edit(APIGatewayHttpApiV2ProxyRequest input, ILambdaContext context)
    {
        var petId = input.PathParameters["petId"];

        var request = JsonSerializer.Deserialize<EditPetRequest>(input.Body, _options)!;

        var response = await _dynamoDBClient.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>() { { "petId", new AttributeValue { S = petId.ToString() } } },
        });

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            return new APIGatewayHttpApiV2ProxyResponse
            {
                StatusCode = 404
            };
        }

        var putItemRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue> {
                {
                        "petId",
                        new AttributeValue {
                        S = petId.ToString(),
                    }
                    },
                    {
                        "type",
                        new AttributeValue {
                        S = request.Type
                        }
                    },
                    {
                        "name",
                        new AttributeValue {
                            S = request.Name
                        }
                }
            }
        };

        await _dynamoDBClient.PutItemAsync(putItemRequest);

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } }
        };
    }
}