# Set the memory, frontier type, level, speed and timeout
MEMORY="-Xmx16g"
FRONTIER="-bfs"
LEVEL="levels/SAFirefly.lvl"
SPEED=500
TIMEOUT=1800000

# Set to 1 to enable debugging
DEBUG=0

# Compile
dotnet build

# Run
if [ $DEBUG == 1 ]; then $RUN_FLAG="-- debug"; else $RUN_FLAG="--configuration Release --verbosity q"; fi
RUN_STRING="dotnet run ${RUN_FLAG}"

java -jar server.jar -l $LEVEL -c "${RUN_STRING}" -g -s $SPEED -t $TIMEOUT
