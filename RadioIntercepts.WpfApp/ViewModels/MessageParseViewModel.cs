using RadioIntercepts.Application.Parsers;
using RadioIntercepts.Application.Parsers.Sources;

public class MessageParserViewModel 
{
    private readonly IMessageParser _parser;
    private readonly IRadioMessageSource _source;

    public MessageParserViewModel(IMessageParser parser, IRadioMessageSource source)
    {
        _parser = parser;
        _source = source;
    }

    public async Task ParseMessageAsync()
    {
        string raw = await _source.GetRawMessageAsync();
        var message = _parser.Parse(raw);

        // Далее обновление свойств ViewModel
    }
}
