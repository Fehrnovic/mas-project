# Set the memory, frontier type, level, speed and timeout
MEMORY="-Xmx16g"
FRONTIER="-greedy"
LEVEL="levels/MAPF00.lvl"
SPEED=500
TIMEOUT=180

# Set to 1 to enable debugging
DEBUG=1

# Compile
dotnet build

# Run
if [ $DEBUG == 1 ]; then DEBUG="-- debug"; else DEBUG=""; fi
RUN_STRING="dotnet run ${DEBUG}"

java -jar server.jar -l $LEVEL -c "${RUN_STRING}" -g -s $SPEED -t $TIMEOUT
