using System.Text;
using pg_protoexport;

namespace BasicSample;

internal static class MarkdownPlantUmlGenerator
{
    public static string GenerateMarkdownPlantUml(List<PostgresPacket> pgPackets)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"""
            # Sequence diagram

            Recorded : {pgPackets[0].Timestamp:O}

            ## Compact diagram

            """);

        markdown.AppendLine(PacketsToMarkdownUml(pgPackets));

        markdown.AppendLine("""
        ## Detailed diagram

        """);
        markdown.AppendLine(PacketsToCompactMarkdownUml(pgPackets));

        return markdown.ToString();
    }    
    static string PacketsToMarkdownUml(List<PostgresPacket> pgPackets)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"""
            ```plantuml
            @startuml
            participant "Client\n{pgPackets[0].SourceAddress}:{pgPackets[0].SourcePort}" as C
            database "Server\n{pgPackets[0].DestinationAddress}:{pgPackets[0].DestinationPort}" as S
            """);

        int packetIndex = 0;
        DateTime initialDate = default;
        DateTime lastDate = default;
        foreach (var packet in pgPackets)
        {
            if (packetIndex == 0)
            {
                initialDate = packet.Timestamp;
                markdown.AppendLine($"group packet {++packetIndex}");
            }
            else
            {
                // delta time
                markdown.Append($"group packet {++packetIndex} [");
                markdown.Append($"{(packet.Timestamp - initialDate).TotalMilliseconds:N1} ms TOTAL");
                markdown.AppendLine($"\\n+{(packet.Timestamp - lastDate).TotalMilliseconds:N1} ms SINCE last]");
            }
            lastDate = packet.Timestamp;

            markdown.AppendLine(PacketToUml(packet));
            markdown.AppendLine("end");
        }

        markdown.AppendLine("""
            @enduml
            ```
            """);

        return markdown.ToString();
    }

    static string PacketsToCompactMarkdownUml(List<PostgresPacket> pgPackets)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"""
            ```plantuml
            @startuml
            participant "Client\n{pgPackets[0].SourceAddress}:{pgPackets[0].SourcePort}" as C
            database "Server\n{pgPackets[0].DestinationAddress}:{pgPackets[0].DestinationPort}" as S
            """);

        foreach (var packet in pgPackets)
        {
            markdown.AppendLine(PacketToCompactUml(packet));
        }

        markdown.AppendLine("""
            @enduml
            ```
            """);

        return markdown.ToString();
    }

    static string? PacketToUml(PostgresPacket packet)
    {
        var strOutBuilder = new StringBuilder();
        List<(PostgresMessageBase Message, int Count)> messagesGrouped = [];
        foreach (var message in packet.Messages)
        {
            if (messagesGrouped.Count > 0 && messagesGrouped[^1].Message.Code == message.Code)
            {
                messagesGrouped[^1] = (messagesGrouped[^1].Message, messagesGrouped[^1].Count + 1);
            }
            else
            {
                messagesGrouped.Add((message, 1));
            }
        }

        foreach (var msg in messagesGrouped)
        {
            strOutBuilder.Append(packet.IsFrontEnd ? "C -> S : " : "S -> C : ");
            strOutBuilder.AppendLine(msg.Count == 1 ? msg.Message.Name : $"{msg.Message.Name} (x{msg.Count})");
        }

        return strOutBuilder.ToString();
    }

    static string? PacketToCompactUml(PostgresPacket packet)
    {
        var strOutBuilder = new StringBuilder();
        List<(PostgresMessageBase Message, int Count)> messagesGrouped = [];
        foreach (var message in packet.Messages)
        {
            if (messagesGrouped.Count > 0 && messagesGrouped[^1].Message.Code == message.Code)
            {
                messagesGrouped[^1] = (messagesGrouped[^1].Message, messagesGrouped[^1].Count + 1);
            }
            else
            {
                messagesGrouped.Add((message, 1));
            }
        }
        strOutBuilder.Append(packet.IsFrontEnd ? "C -> S : " : "S -> C : ");
        strOutBuilder.AppendLine(string.Join(" / ", messagesGrouped.Select(m =>
                                    m.Count == 1 ?
                                    m.Message.Name
                                    : $"{m.Message.Name} (x{m.Count})"
                                    )));

        return strOutBuilder.ToString();
    }
}
