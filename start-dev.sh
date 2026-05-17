#!/bin/bash

# Start Budget App Development Servers

echo "🚀 Starting Budget App..."

# Start backend in background
echo "📦 Starting Backend API..."
cd /workspace/backend/BudgetApp.Api
dotnet run > /tmp/backend.log 2>&1 &
BACKEND_PID=$!
echo "   Backend PID: $BACKEND_PID"

# Wait a moment for backend to start
sleep 5

# Start frontend in background
echo "⚛️  Starting Frontend..."
cd /workspace/frontend
npm run dev > /tmp/frontend.log 2>&1 &
FRONTEND_PID=$!
echo "   Frontend PID: $FRONTEND_PID"

echo ""
echo "✅ Services started!"
echo ""
echo "📊 Access points:"
echo "   Backend:  http://localhost:5000"
echo "   Swagger:  http://localhost:5000/swagger"
echo "   Frontend: http://localhost:5173"
echo "   MongoDB:  mongodb://mongo:27017"
echo ""
echo "📝 Logs:"
echo "   Backend:  tail -f /tmp/backend.log"
echo "   Frontend: tail -f /tmp/frontend.log"
echo ""
echo "🛑 To stop services:"
echo "   kill $BACKEND_PID $FRONTEND_PID"
